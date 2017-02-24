using System;
using System.Collections.Generic;
using Models.Core;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Models.WholeFarm.Groupings;
using Models.WholeFarm.Resources;

namespace Models.WholeFarm.Groupings
{
	/// <summary>
	/// Herd list extensions
	/// </summary>
	public static class ListFilterExtensions
	{
		/// <summary>
		/// Filter extensions for labour
		/// </summary>
		public static List<LabourType> Filter(this IEnumerable<LabourType> individuals, Model filterGroup)
		{
			var rules = new List<Rule>();
			foreach (LabourFilter filter in filterGroup.Children.Where(a => a.GetType() == typeof(LabourFilter)))
			{
				ExpressionType op = (ExpressionType)Enum.Parse(typeof(ExpressionType), filter.Operator.ToString());
				// create rule list
				rules.Add(new Rule(filter.Parameter.ToString(), op, filter.Value));
			}
			var compiledRulesList = CompileRule(new List<LabourType>(), rules);
			return GetItemsThatMatchAll<LabourType>(individuals, compiledRulesList).ToList<LabourType>();
		}

		/// <summary>
		/// Filter extensions for herd list
		/// </summary>
		public static List<Ruminant> Filter(this IEnumerable<Ruminant> individuals, Model filterGroup)
		{
			var rules = new List<Rule>();
			foreach (var child in filterGroup.Children)
			{
				if (child.GetType() == typeof(RuminantFilter))
				{
					RuminantFilter filter = child as RuminantFilter;
					ExpressionType op = (ExpressionType)Enum.Parse(typeof(ExpressionType), filter.Operator.ToString());
					// create rule list
					rules.Add(new Rule(filter.Parameter.ToString(), op, filter.Value));
				}
			}

			var compiledRulesList = CompileRule(new List<Ruminant>(), rules);
			return GetItemsThatMatchAll<Ruminant>(individuals, compiledRulesList).ToList<Ruminant>();
		}

		/// <summary>
		/// Filter extensions for other animals cohort list
		/// </summary>
		public static List<OtherAnimalsTypeCohort> Filter(this IEnumerable<OtherAnimalsTypeCohort> individuals, Model filterGroup)
		{
			var rules = new List<Rule>();
			foreach (var child in filterGroup.Children)
			{
				if (child.GetType() == typeof(OtherAnimalsFilter))
				{
					OtherAnimalsFilter filter = child as OtherAnimalsFilter;
					ExpressionType op = (ExpressionType)Enum.Parse(typeof(ExpressionType), filter.Operator.ToString());
					// create rule list
					rules.Add(new Rule(filter.Parameter.ToString(), op, filter.Value));
				}
			}

			var compiledRulesList = CompileRule(new List<OtherAnimalsTypeCohort>(), rules);
			return GetItemsThatMatchAll<OtherAnimalsTypeCohort>(individuals, compiledRulesList).ToList<OtherAnimalsTypeCohort>();
		}

		private class Rule
		{
			public string ComparisonPredicate { get; set; }
			public System.Linq.Expressions.ExpressionType ComparisonOperator { get; set; }
			public string ComparisonValue { get; set; }

			public Rule(string comparisonPredicate, System.Linq.Expressions.ExpressionType comparisonOperator, string comparisonValue)
			{
				ComparisonPredicate = comparisonPredicate;
				ComparisonOperator = comparisonOperator;
				ComparisonValue = comparisonValue;
			}
		}

		private static IEnumerable<T> GetItemsThatMatchAny<T>(this IEnumerable<T> source, IEnumerable<Func<T, bool>> predicates)
		{
			return source.Where(t => predicates.Any(predicate => predicate(t)));
		}

		private static IEnumerable<T> GetItemsThatMatchAll<T>(this IEnumerable<T> source, IEnumerable<Func<T, bool>> predicates)
		{
			return source.Where(t => predicates.All(predicate => predicate(t)));
		}

		private static List<Func<T, bool>> CompileRule<T>(List<T> targetEntity, List<Rule> rules)
		{
			// Credit for this function goes to Cole Francis, Architect
			// The pre-compiled rules type
			// https://mobiusstraits.com/2015/08/12/expression-trees/

			var compiledRules = new List<Func<T, bool>>();

			// Loop through the rules and compile them against the properties of the supplied shallow object 
			rules.ForEach(rule =>
			{
				var genericType = Expression.Parameter(typeof(T));
				var key = Expression.Property(genericType, rule.ComparisonPredicate);
				var propertyType = typeof(T).GetProperty(rule.ComparisonPredicate).PropertyType;
				object ce;
				if (propertyType.BaseType.Name == "Enum")
				{
					ce = Enum.Parse(propertyType, rule.ComparisonValue, true);
				}
				else
				{
					ce = Convert.ChangeType(rule.ComparisonValue, propertyType);
				}
//				var value = Expression.Constant(Convert.ChangeType(rule.ComparisonValue, propertyType));
				var value = Expression.Constant(ce);
				var binaryExpression = Expression.MakeBinary(rule.ComparisonOperator, key, value);

				compiledRules.Add(Expression.Lambda<Func<T, bool>>(binaryExpression, genericType).Compile());
			});

			// Return the compiled rules to the caller
			return compiledRules;
		}

	}
}
