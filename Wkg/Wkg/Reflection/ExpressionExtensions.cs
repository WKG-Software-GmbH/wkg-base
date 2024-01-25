using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace Wkg.Reflection;

/// <summary>
/// Provides extension methods for <see cref="Expression"/>.
/// </summary>
public static class ExpressionExtensions
{
    /// <summary>
    /// Gets the <see cref="MemberInfo"/> for the specified member access expression.
    /// </summary>
    /// <param name="memberAccessExpression">The member access expression, e.g., <c>foo => foo.Bar</c>.</param>
    /// <returns>The <see cref="MemberInfo"/> for the specified member access expression.</returns>
    public static MemberInfo GetMemberAccess(this LambdaExpression memberAccessExpression)
        => GetInternalMemberAccess<MemberInfo>(memberAccessExpression);

    private static TMemberInfo GetInternalMemberAccess<TMemberInfo>(this LambdaExpression memberAccessExpression)
        where TMemberInfo : MemberInfo
    {
        ParameterExpression parameterExpression = memberAccessExpression.Parameters[0];
        TMemberInfo? memberInfo = parameterExpression.MatchSimpleMemberAccess<TMemberInfo>(memberAccessExpression.Body) 
            ?? throw new InvalidOperationException($"Unable to determine member from {memberAccessExpression.Body}.");

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
    /// Matches a simple member access expression, e.g. <c>x => x.Property</c>.
    /// </summary>
    /// <param name="parameterExpression">The parameter expression.</param>
    /// <param name="memberAccessExpression">The member access expression.</param>
    /// <returns>The <see cref="MemberInfo"/> of the member access expression, or <c>null</c> if the expression is not a simple member access expression.</returns>
    public static TMemberInfo? MatchSimpleMemberAccess<TMemberInfo>(
        this Expression parameterExpression,
        Expression memberAccessExpression)
        where TMemberInfo : MemberInfo
    {
        IReadOnlyList<TMemberInfo>? memberInfos = MatchMemberAccess<TMemberInfo>(parameterExpression, memberAccessExpression);

        return memberInfos?.Count == 1 ? memberInfos[0] : null;
    }

    private static List<TMemberInfo>? MatchMemberAccess<TMemberInfo>(
        this Expression parameterExpression,
        Expression memberAccessExpression)
        where TMemberInfo : MemberInfo
    {
        List<TMemberInfo> memberInfos = [];

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

    /// <summary>
    /// Removes any <see cref="ExpressionType.TypeAs"/> expressions from the specified <paramref name="expression"/>.
    /// </summary>
    /// <param name="expression">The expression.</param>
    public static Expression? RemoveTypeAs(this Expression? expression)
    {
        while (expression?.NodeType == ExpressionType.TypeAs)
        {
            expression = ((UnaryExpression)RemoveConvert(expression)).Operand;
        }

        return expression;
    }

    /// <summary>
    /// Removes any <see cref="ExpressionType.Convert"/> or <see cref="ExpressionType.ConvertChecked"/> expressions from the specified <paramref name="expression"/>.
    /// </summary>
    /// <param name="expression">The expression.</param>
    [return: NotNullIfNotNull(nameof(expression))]
    public static Expression? RemoveConvert(Expression? expression)
    {
        if (expression is UnaryExpression unaryExpression
            && (expression.NodeType == ExpressionType.Convert
                || expression.NodeType == ExpressionType.ConvertChecked))
        {
            return RemoveConvert(unaryExpression.Operand);
        }

        return expression;
    }

    /// <summary>
    /// Attempts to match a simple member access expression, e.g. <c>x => x.Property1.Property2</c> or <c>() => x.Field1.Field2</c>.
    /// </summary>
    /// <param name="memberAccessExpression">The member access expression.</param>
    /// <param name="member">(Output) The <see cref="MemberInfo"/> of the innermost expression, e.g. <c>Property2</c> in <c>x => x.Property1.Property2</c></param>
    /// <returns><see langword="true"/> if the expression was successfully matched; otherwise, <see langword="false"/>.</returns>
    public static bool TryMatchDirectMemberAccess(this Expression memberAccessExpression, [NotNullWhen(true)] out MemberInfo? member)
    {
        Expression? unwrappedExpression = RemoveTypeAs(RemoveConvert(memberAccessExpression));
        if (unwrappedExpression is MemberExpression memberExpression)
        {
            member = memberExpression.Member;
            return true;
        }
        member = null;
        return false;
    }
}