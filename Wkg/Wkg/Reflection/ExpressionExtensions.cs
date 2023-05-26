using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace Wkg.Reflection;

public static class ExpressionExtensions
{
    public static MemberInfo GetMemberAccess(this LambdaExpression memberAccessExpression)
        => GetInternalMemberAccess<MemberInfo>(memberAccessExpression);

    private static TMemberInfo GetInternalMemberAccess<TMemberInfo>(this LambdaExpression memberAccessExpression)
        where TMemberInfo : MemberInfo
    {
        ParameterExpression parameterExpression = memberAccessExpression.Parameters[0];
        TMemberInfo? memberInfo = parameterExpression.MatchSimpleMemberAccess<TMemberInfo>(memberAccessExpression.Body);

        if (memberInfo == null)
        {
            throw new InvalidOperationException($"Unable to determine member from {memberAccessExpression.Body}.");
        }

        Type? declaringType = memberInfo.DeclaringType;
        Type parameterType = parameterExpression.Type;

        if (declaringType != null
            && declaringType != parameterType
            && declaringType.IsInterface
            && declaringType.IsAssignableFrom(parameterType)
            && memberInfo is PropertyInfo propertyInfo)
        {
            MethodInfo? propertyGetter = propertyInfo.GetMethod;
            InterfaceMapping interfaceMapping = parameterType.GetTypeInfo().GetRuntimeInterfaceMap(declaringType);
            int index = Array.FindIndex(interfaceMapping.InterfaceMethods, p => p.Equals(propertyGetter));
            MethodInfo targetMethod = interfaceMapping.TargetMethods[index];
            foreach (PropertyInfo runtimeProperty in parameterType.GetRuntimeProperties())
            {
                if (targetMethod.Equals(runtimeProperty.GetMethod))
                {
                    return (TMemberInfo)(object)runtimeProperty;
                }
            }
        }

        return memberInfo;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static IReadOnlyList<TMemberInfo>? MatchMemberAccessList<TMemberInfo>(
        this LambdaExpression lambdaExpression,
        Func<Expression, Expression, TMemberInfo?> memberMatcher)
        where TMemberInfo : MemberInfo
    {
        ParameterExpression parameterExpression = lambdaExpression.Parameters[0];

        if (RemoveConvert(lambdaExpression.Body) is NewExpression newExpression)
        {
            List<TMemberInfo> memberInfos
                = (List<TMemberInfo>)newExpression
                    .Arguments
                    .Select(a => memberMatcher(a, parameterExpression))
                    .Where(p => p != null)
                    .ToList()!;

            return memberInfos.Count != newExpression.Arguments.Count ? null : memberInfos;
        }

        TMemberInfo? memberPath = memberMatcher(lambdaExpression.Body, parameterExpression);

        return memberPath != null ? new[] { memberPath } : null;
    }

    public static TMemberInfo? MatchSimpleMemberAccess<TMemberInfo>(
        this Expression parameterExpression,
        Expression memberAccessExpression)
        where TMemberInfo : MemberInfo
    {
        IReadOnlyList<TMemberInfo>? memberInfos = MatchMemberAccess<TMemberInfo>(parameterExpression, memberAccessExpression);

        return memberInfos?.Count == 1 ? memberInfos[0] : null;
    }

    private static IReadOnlyList<TMemberInfo>? MatchMemberAccess<TMemberInfo>(
        this Expression parameterExpression,
        Expression memberAccessExpression)
        where TMemberInfo : MemberInfo
    {
        List<TMemberInfo> memberInfos = new();

        Expression? unwrappedExpression = RemoveTypeAs(RemoveConvert(memberAccessExpression));
        do
        {
            MemberExpression? memberExpression = unwrappedExpression as MemberExpression;

            if (memberExpression?.Member is not TMemberInfo memberInfo)
            {
                return null;
            }

            memberInfos.Insert(0, memberInfo);

            unwrappedExpression = RemoveTypeAs(RemoveConvert(memberExpression.Expression));
        }
        while (unwrappedExpression != parameterExpression);

        return memberInfos;
    }

    public static Expression? RemoveTypeAs(this Expression? expression)
    {
        while (expression?.NodeType == ExpressionType.TypeAs)
        {
            expression = ((UnaryExpression)RemoveConvert(expression)).Operand;
        }

        return expression;
    }

    [return: NotNullIfNotNull(nameof(expression))]
    private static Expression? RemoveConvert(Expression? expression)
    {
        if (expression is UnaryExpression unaryExpression
            && (expression.NodeType == ExpressionType.Convert
                || expression.NodeType == ExpressionType.ConvertChecked))
        {
            return RemoveConvert(unaryExpression.Operand);
        }

        return expression;
    }
}