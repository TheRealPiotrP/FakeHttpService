using System;
using System.Linq.Expressions;
using System.Reflection;

namespace FakeHttpService
{
    public class ConstantMemberEvaluationVisitor : ExpressionVisitor
    {
        private class CustomMethodCallExpression : Expression
        {
            public override ExpressionType NodeType => _original.NodeType;
            public override Type Type => _original.Type;


            private readonly MethodCallExpression _original;
            private readonly string _label;

            public CustomMethodCallExpression(MethodCallExpression original, string label = null)
            {
                _original = original;
                _label = label;
            }

            public override string ToString()
            {
                return _label ?? _original.Method.ReturnType.ToString();
            }
        }
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var methodName = node.Method.Name;
            var declaringType = node.Method.DeclaringType.FullName;
            var filterName = typeof(FilterBuilders.RequestFilterExpressionBuilder).FullName;

            switch (methodName)
            {
                case "Body" when declaringType == filterName:
                    return  new CustomMethodCallExpression(node, node.Method.IsGenericMethod? $"Body<{node.Method.ReturnType}>": "Body");
                case "Json" when declaringType == filterName:
                    return new CustomMethodCallExpression(node,  "Body<Json>");
            }
            
            return base.VisitMethodCall(node);
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Expression.NodeType != ExpressionType.Constant) return base.VisitMember(node);
            var constantExpression = ((ConstantExpression)node.Expression).Value;
            object constantValue = null;

            switch (node.Member.MemberType)
            {
                case MemberTypes.Property:
                    constantValue = ((PropertyInfo)node.Member).GetValue(constantExpression, null);
                    break;
                case MemberTypes.Field:
                    constantValue = ((FieldInfo)node.Member).GetValue(constantExpression);
                    break;
            }

            return constantValue != null ? Expression.Constant(constantValue, node.Type) : base.VisitMember(node);
        }
    }
}