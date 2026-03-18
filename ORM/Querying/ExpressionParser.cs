using ORM.Mapping;
using System.Linq.Expressions;

namespace ORM.Querying;



public class ExpressionParser
{
    private int _parameterCounter;
    private readonly Dictionary<string, object> _parameters = new();
    private readonly EntityMetadata _metadata;

    public ExpressionParser(EntityMetadata metadata)
    {
        _metadata = metadata;
    }

    public (string Sql, Dictionary<string, object> Parameters) Parse<T>(Expression<Func<T, bool>> predicate)
    {
        _parameters.Clear();
        _parameterCounter = 0;

        var sql = predicate.Body switch
        {
            BinaryExpression binary => ParseBinaryExpression(binary),
            MethodCallExpression methodCall => ParseMethodCallExpression(methodCall),
            MemberExpression member => ParseMemberExpression(member) + " = TRUE",
            _ => throw new NotSupportedException($"Expression type {predicate.Body.NodeType} is not supported.")
        };

        return (sql, new Dictionary<string, object>(_parameters));
    }

    private string ParseBinaryExpression(BinaryExpression expression)
    {
        if (expression.NodeType == ExpressionType.AndAlso)
        {
            return $"({ParseExpression(expression.Left)} AND {ParseExpression(expression.Right)})";
        }

        if (expression.NodeType == ExpressionType.OrElse)
        {
            return $"({ParseExpression(expression.Left)} OR {ParseExpression(expression.Right)})";
        }

        var leftSql = ParseExpression(expression.Left);
        var rightSql = ParseExpression(expression.Right);
        var op = GetOperator(expression.NodeType);

        return $"{leftSql} {op} {rightSql}";
    }

    private string ParseMethodCallExpression(MethodCallExpression expression)
    {
        var methodName = expression.Method.Name;

        if (methodName == "Contains" && expression.Object != null)
        {
            var member = ParseExpression(expression.Object);
            var value = EvaluateExpression(expression.Arguments[0]);
            var paramName = CreateParameter($"%{value}%");
            return $"{member} LIKE {paramName}";
        }

        if (methodName == "StartsWith" && expression.Object != null)
        {
            var member = ParseExpression(expression.Object);
            var value = EvaluateExpression(expression.Arguments[0]);
            var paramName = CreateParameter($"{value}%");
            return $"{member} LIKE {paramName}";
        }

        if (methodName == "EndsWith" && expression.Object != null)
        {
            var member = ParseExpression(expression.Object);
            var value = EvaluateExpression(expression.Arguments[0]);
            var paramName = CreateParameter($"%{value}");
            return $"{member} LIKE {paramName}";
        }

        if (methodName == "Equals")
        {
            var left = ParseExpression(expression.Object ?? expression.Arguments[0]);
            var right = ParseExpression(expression.Arguments[expression.Object == null ? 1 : 0]);
            return $"{left} = {right}";
        }

        throw new NotSupportedException($"Method {methodName} is not supported in expressions.");
    }

    private string ParseMemberExpression(MemberExpression expression)
    {
        if (expression.Expression is ParameterExpression)
        {
            return ResolveColumnName(expression.Member.Name);
        }

        var value = EvaluateExpression(expression);
        return CreateParameter(value);
    }

    private string ResolveColumnName(string propertyName)
    {
        var prop = _metadata.Properties.FirstOrDefault(p => p.PropertyName == propertyName);
        return prop?.ColumnName ?? propertyName;
    }

    private string ParseExpression(Expression expression)
    {
        return expression switch
        {
            BinaryExpression binary => ParseBinaryExpression(binary),
            MemberExpression member => ParseMemberExpression(member),
            ConstantExpression constant => CreateParameter(constant.Value),
            MethodCallExpression methodCall => ParseMethodCallExpression(methodCall),
            UnaryExpression unary => ParseUnaryExpression(unary),
            _ => throw new NotSupportedException($"Expression type {expression.NodeType} is not supported.")
        };
    }

    private string ParseUnaryExpression(UnaryExpression expression)
    {
        if (expression.NodeType == ExpressionType.Not)
        {
            return $"NOT ({ParseExpression(expression.Operand)})";
        }

        if (expression.NodeType == ExpressionType.Convert)
        {
            return ParseExpression(expression.Operand);
        }

        throw new NotSupportedException($"Unary expression {expression.NodeType} is not supported.");
    }

    private static object? EvaluateExpression(Expression expression)
    {
        if (expression is ConstantExpression constant)
        {
            return constant.Value;
        }

        var lambda = Expression.Lambda(expression);
        return lambda.Compile().DynamicInvoke();
    }

    private string CreateParameter(object? value)
    {
        var paramName = $"@p{_parameterCounter++}";
        _parameters[paramName] = value ?? DBNull.Value;
        return paramName;
    }

    private static string GetOperator(ExpressionType nodeType)
    {
        return nodeType switch
        {
            ExpressionType.Equal => "=",
            ExpressionType.NotEqual => "!=",
            ExpressionType.GreaterThan => ">",
            ExpressionType.GreaterThanOrEqual => ">=",
            ExpressionType.LessThan => "<",
            ExpressionType.LessThanOrEqual => "<=",
            _ => throw new NotSupportedException($"Operator {nodeType} is not supported.")
        };
    }
}
