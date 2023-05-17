using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace ModToolFramework.Utils
{
    /// <summary>
    /// Contains static utilities for performing code-generation, reflection tasks, etc in performant ways.
    /// </summary>
    public static class DynamicCodeUtils
    {
        public class ConstructorNotFoundException : Exception
        {
            public ConstructorNotFoundException(Type objectType, params Type[] parameterTypes) :
                base("The constructor " + objectType.GetDisplayName() + "(" + GetTypeNames(parameterTypes) + ") was not found.") {
            }

            private static string GetTypeNames(IReadOnlyList<Type> parameterTypes) {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < parameterTypes.Count; i++) {
                    if (i > 0)
                        sb.Append(", ");
                    sb.Append(parameterTypes[i]?.GetDisplayName() ?? "null");
                }

                return sb.ToString();
            }
        }
        
        /// <summary>
        /// Creates a constructor for the provided object with the provided arguments.
        /// This should be cached for maximum performance.
        /// </summary>
        /// <typeparam name="TParamA">The first type accepted by the constructor.</typeparam>
        /// <typeparam name="TObject">The type of object to construct.</typeparam>
        /// <returns>constructorFunction</returns>
        /// <exception cref="ConstructorNotFoundException">Thrown if no constructor exists matching the given parameters.</exception>
        public static Func<TParamA, TObject> CreateConstructor<TParamA, TObject>() {
            Type objectType = typeof(TObject);
            var constructor = objectType.GetConstructor(new[] {typeof(TParamA)});
            if (constructor == null)
                throw new ConstructorNotFoundException(objectType, typeof(TParamA));

            var arg0 = Expression.Parameter(typeof(TParamA), "arg0");
            NewExpression newExpression = Expression.New(constructor, arg0);
            
            var callExpression = Expression.Lambda<Func<TParamA, TObject>>(newExpression, arg0); 
            return callExpression.Compile();
        }
        
        /// <summary>
        /// Creates a constructor for the provided object with the provided arguments.
        /// This should be cached for maximum performance.
        /// </summary>
        /// <typeparam name="TParamA">The first type accepted by the constructor.</typeparam>
        /// <typeparam name="TParamB">The second type accepted by the constructor.</typeparam>
        /// <typeparam name="TObject">The type of object to construct.</typeparam>
        /// <returns>constructorFunction</returns>
        /// <exception cref="ConstructorNotFoundException">Thrown if no constructor exists matching the given parameters.</exception>
        public static Func<TParamA, TParamB, TObject> CreateConstructor<TParamA, TParamB, TObject>() {
            Type objectType = typeof(TObject);
            var constructor = objectType.GetConstructor(new[] {typeof(TParamA), typeof(TParamB)});
            if (constructor == null)
                throw new ConstructorNotFoundException(objectType, typeof(TParamA), typeof(TParamB));

            var arg0 = Expression.Parameter(typeof(TParamA), "arg0");
            var arg1 = Expression.Parameter(typeof(TParamB), "arg1");
            NewExpression newExpression = Expression.New(constructor, arg0, arg1);
            
            var callExpression = Expression.Lambda<Func<TParamA, TParamB, TObject>>(newExpression, arg0, arg1); 
            return callExpression.Compile();
        }

        /// <summary>
        /// Creates a constructor for the provided object with the provided arguments.
        /// This should be cached for maximum performance.
        /// </summary>
        /// <typeparam name="TParamA">The first type accepted by the constructor.</typeparam>
        /// <typeparam name="TParamB">The second type accepted by the constructor.</typeparam>
        /// <typeparam name="TParamC">The third type accepted by the constructor.</typeparam>
        /// <typeparam name="TObject">The type of object to construct.</typeparam>
        /// <returns>constructorFunction</returns>
        /// <exception cref="ConstructorNotFoundException">Thrown if no constructor exists matching the given parameters.</exception>
        public static Func<TParamA, TParamB, TParamC, TObject> CreateConstructor<TParamA, TParamB, TParamC, TObject>() {
            Type objectType = typeof(TObject);
            var constructor = objectType.GetConstructor(new[] {typeof(TParamA), typeof(TParamB), typeof(TParamC)});
            if (constructor == null)
                throw new ConstructorNotFoundException(objectType, typeof(TParamA), typeof(TParamB), typeof(TParamC));

            var arg0 = Expression.Parameter(typeof(TParamA), "arg0");
            var arg1 = Expression.Parameter(typeof(TParamB), "arg1");
            var arg2 = Expression.Parameter(typeof(TParamC), "arg2");
            NewExpression newExpression = Expression.New(constructor, arg0, arg1, arg2);
            
            var callExpression = Expression.Lambda<Func<TParamA, TParamB, TParamC, TObject>>(newExpression, arg0, arg1, arg2); 
            return callExpression.Compile();
        }
        
        /// <summary>
        /// Creates a getter method for a private field.
        /// </summary>
        /// <param name="fieldName">The field to create the getter for.</param>
        /// <typeparam name="TType">The type containing the field.</typeparam>
        /// <typeparam name="TReturn">The type of the field.</typeparam>
        /// <returns>getterFunction</returns>
        public static Func<TType, TReturn> CreateGetter<TType, TReturn>(string fieldName) {
            ParameterExpression expression = Expression.Parameter(typeof(TType), "value");
            return Expression.Lambda<Func<TType, TReturn>>(
                    Expression.PropertyOrField(expression, fieldName), expression)
                .Compile();
        }
        
        /// <summary>
        /// Creates a setter method for a private field.
        /// </summary>
        /// <param name="fieldName">The field to create the setter for.</param>
        /// <typeparam name="TType">The type containing the field.</typeparam>
        /// <typeparam name="TReturn">The type of the field.</typeparam>
        /// <returns>setterFunction</returns>
        public static Action<TType, TReturn> CreateSetter<TType, TReturn>(string fieldName) {
            ParameterExpression paramExpression = Expression.Parameter(typeof(TType));
            ParameterExpression paramExpression2 = Expression.Parameter(typeof(TReturn), fieldName);
            return Expression.Lambda<Action<TType, TReturn>>(
                    Expression.Assign(Expression.Field(paramExpression, fieldName), paramExpression2), paramExpression, paramExpression2)
                .Compile();
        }
    }
}