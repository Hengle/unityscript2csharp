using Boo.Lang.Compiler.Ast;
using Boo.Lang.Compiler.Steps;
using Boo.Lang.Compiler.TypeSystem;

namespace UnityScript2CSharp.Steps
{
    internal class InjectTypeOfExpressionsInArgumentsOfSystemType : AbstractTransformerCompilerStep
    {
        private Expression _currentArgument;

        public override void OnMethodInvocationExpression(MethodInvocationExpression node)
        {
            if (node.Target.Entity == null || (node.Target.Entity.EntityType != EntityType.Method && node.Target.Entity.EntityType != EntityType.Constructor))
                return;

            for (int i = 0; i < node.Arguments.Count; i++)
            {
                _currentArgument = node.Arguments[i];
                if (_currentArgument.ExpressionType.ElementType != TypeSystemServices.TypeType)
                    continue;

                _currentArgument.Accept(this);
            }

            _currentArgument = null;
            base.OnMethodInvocationExpression(node);
        }

        public override void OnMemberReferenceExpression(MemberReferenceExpression node)
        {
            if (HasImplictTypeOfExpression(node))
            {
                node.ParentNode.Replace(node, CodeBuilder.CreateTypeofExpression( TypeSystemServices.GetType(node)));
                return;
            }

            base.OnMemberReferenceExpression(node);
        }

        public override void OnReferenceExpression(ReferenceExpression node)
        {
            if (!HasImplictTypeOfExpression(node))
                return;

            node.ParentNode.Replace(node, CodeBuilder.CreateTypeofExpression(TypeSystemServices.GetType(node)));
        }

        public override void OnArrayLiteralExpression(ArrayLiteralExpression node)
        {
            if (node.Type == null)
            {
                node.Type = new ArrayTypeReference(new SimpleTypeReference(node.ExpressionType.ElementType.FullName));
                node.Type.ElementType.Entity = node.ExpressionType.ElementType;
            }

            base.OnArrayLiteralExpression(node);
        }

        public override void OnBinaryExpression(BinaryExpression node)
        {
            if (node.Operator == BinaryOperatorType.Assign && node.Left.ExpressionType == TypeSystemServices.TypeType && node.Right.ExpressionType.EntityType == EntityType.Type && (node.Right.Entity != null && node.Right.Entity.EntityType == EntityType.Type))
            {
                node.Replace(node.Right, CodeBuilder.CreateTypeofExpression(TypeSystemServices.GetType(node.Right)));
            }
            else if (node.Operator == BinaryOperatorType.TypeTest)
            {
                var typeofExpression = (TypeofExpression) node.Right;
                node.Replace(node.Right, CodeBuilder.CreateReference(typeofExpression.Type.Entity));
            }

            base.OnBinaryExpression(node);
        }

        private bool HasImplictTypeOfExpression(ReferenceExpression node)
        {
            if (_currentArgument != null)
            {
                if (node.Entity.EntityType != EntityType.Type)
                    return false;

                return node.ParentNode.NodeType == NodeType.MethodInvocationExpression || node.ParentNode.NodeType == NodeType.ArrayLiteralExpression;
            }

            if (node.ParentNode.NodeType == NodeType.YieldStatement)
                return node.Entity.EntityType == EntityType.Type;

            return node.ParentNode.NodeType == NodeType.Attribute;
        }
    }
}
