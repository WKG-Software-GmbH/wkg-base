using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wkg.Common.Extensions;

public static class StringBuilderExtensions
{
    public static StringBuilder AppendIndent(this StringBuilder builder, int indentLevel)
    {
        builder.Append(' ', indentLevel * 2);
        return builder;
    }
}
