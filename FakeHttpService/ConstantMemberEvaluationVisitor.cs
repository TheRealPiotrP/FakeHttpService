using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace FakeHttpService
{
    public class ConstantMemberEvaluationVisitor : ExpressionVisitor
    {
        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Expression.NodeType != ExpressionType.Constant) return base.VisitMember(node);
            var constantExpression = ((ConstantExpression) node.Expression).Value;
            object constantValue = null;

            switch (node.Member.MemberType)
            {
                case MemberTypes.Property:
                    constantValue = ((PropertyInfo) node.Member).GetValue(constantExpression, null);
                    break;
                case MemberTypes.Field:
                    constantValue = ((FieldInfo) node.Member).GetValue(constantExpression);
                    break;
            }

            return constantValue != null ? Expression.Constant(constantValue, node.Type) : base.VisitMember(node);
        }
    }
}