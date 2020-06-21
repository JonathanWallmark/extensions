// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Internal;

namespace Microsoft.EntityFrameworkCore.Metadata.Internal
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public class ClrPropertyGetterFactory : ClrAccessorFactory<IClrPropertyGetter>
    {
        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public override IClrPropertyGetter Create(IPropertyBase property)
            => property as IClrPropertyGetter ?? Create(property.GetMemberInfo(forMaterialization: false, forSet: false), property);

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        protected override IClrPropertyGetter CreateGeneric<TEntity, TValue, TNonNullableEnumValue>(
            MemberInfo memberInfo, IPropertyBase propertyBase)
        {
            var entityParameter = Expression.Parameter(typeof(TEntity), "entity");

            Expression readExpression;
            if (memberInfo.DeclaringType.IsAssignableFrom(typeof(TEntity)))
            {
                readExpression = CreateMemberAccess(entityParameter);
            }
            else
            {
                // This path handles properties that exist only on proxy types and so only exist if the instance is a proxy
                var converted = Expression.Variable(memberInfo.DeclaringType, "converted");

                readExpression = Expression.Block(
                    new[] { converted },
                    new List<Expression>
                    {
                        Expression.Assign(
                            converted,
                            Expression.TypeAs(entityParameter, memberInfo.DeclaringType)),
                        Expression.Condition(
                            Expression.ReferenceEqual(converted, Expression.Constant(null)),
                            Expression.Default(memberInfo.GetMemberType()),
                            CreateMemberAccess(converted))
                    });
            }

            var hasDefaultValueExpression = readExpression.MakeHasDefaultValue<TValue>(propertyBase);

            if (readExpression.Type != typeof(TValue))
            {
                readExpression = Expression.Condition(
                    hasDefaultValueExpression,
                    Expression.Constant(default(TValue), typeof(TValue)),
                    Expression.Convert(readExpression, typeof(TValue)));
            }

            return new ClrPropertyGetter<TEntity, TValue>(
                Expression.Lambda<Func<TEntity, TValue>>(readExpression, entityParameter).Compile(),
                Expression.Lambda<Func<TEntity, bool>>(hasDefaultValueExpression, entityParameter).Compile());

            Expression CreateMemberAccess(Expression parameter)
            {
                return propertyBase?.IsIndexerProperty() == true
                    ? Expression.MakeIndex(
                        parameter, (PropertyInfo)memberInfo, new List<Expression>() { Expression.Constant(propertyBase.Name) })
                    : (Expression)Expression.MakeMemberAccess(parameter, memberInfo);
            }
        }
    }
}