using APSIM.Shared.Utilities;
using MarkdownDeep;
using Models.Core;
using Models.Functions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UserInterface.Views;

namespace UserInterface.Presenters
{
    public class DocumentationPresenter : IPresenter
    {
        private IHTMLView view;
        private ExplorerPresenter presenter;
        private IModel model;

        private Markdown markdown = new Markdown();

        public DocumentationPresenter()
        {
            markdown.ExtraMode = true;
            markdown.SafeMode = true;
        }

        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.view = view as IHTMLView;
            this.model = model as IModel;
            this.presenter = explorerPresenter;

            PopulateView();
        }

        private void PopulateView()
        {
            string html = DocumentModel(model);
            view.SetContents(html, false, false);
        }

        private string DocumentModel(IModel model)
        {
            StringBuilder html = new StringBuilder();
            html.AppendLine("<html><body>");

            html.AppendLine($"<h1>{model.Name} Description</h1>");
            html.AppendLine("<h2>General Description</h2>");
            string summary = markdown.Transform(AutoDocumentation.GetSummary(model.GetType()));
            html.Append($"<p>{summary}</p>");

            string remarks = markdown.Transform(AutoDocumentation.GetRemarks(model.GetType()));
            if (!string.IsNullOrEmpty(remarks))
            {
                html.AppendLine("<h2>Remarks</h2>");
                html.AppendLine($"<p>{remarks}</p>");
            }

            string typeName = model.GetType().Name;
            html.AppendLine($"<h1>{model.Name} Configuration</h1>");
            html.AppendLine($"<h2>Inputs</h2>");
            //html.AppendLine($"<h3>Fixed Parameters</h3>");
            //html.AppendLine("<p>todo - requires GridView</p>");

            html.AppendLine("<h3>Variable Parameters</h2>");
            DataTable functionTable = GetDependencies(model, m => typeof(IFunction).IsAssignableFrom(GetMemberType(m)));
            html.AppendLine(DataTableUtilities.ToHTML(functionTable, true));

            html.AppendLine("<h3>Other dependencies");
            DataTable depsTable = GetDependencies(model, m => !typeof(IFunction).IsAssignableFrom(GetMemberType(m)));
            html.AppendLine(DataTableUtilities.ToHTML(depsTable, true));

            DataTable publicMethods = GetPublicMethods(model);
            if (publicMethods.Rows.Count > 0)
            {
                html.AppendLine("<h2>Public Methods</h2>");
                html.AppendLine(DataTableUtilities.ToHTML(publicMethods, true));
            }

            DataTable events = GetEvents(model);
            if (events.Rows.Count > 0)
            {
                html.AppendLine("<h2>Events</h2>");
                html.AppendLine(DataTableUtilities.ToHTML(events, true));
            }

            DataTable outputs = GetOutputs(model);
            if (outputs.Rows.Count > 0)
            {
                html.AppendLine("<h2>Model Outputs</h2>");
                html.AppendLine(DataTableUtilities.ToHTML(outputs, true));
            }

            html.Append("</body></html>");

            return html.ToString();
        }

        private DataTable GetEvents(IModel model)
        {
            DataTable table = new DataTable("Public Events");
            table.Columns.Add(new DataColumn("Name", typeof(string)));
            table.Columns.Add(new DataColumn("Delegate Type", typeof(string)));
            table.Columns.Add(new DataColumn("Description", typeof(string)));
            table.Columns.Add(new DataColumn("Remarks", typeof(string)));

            BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy;
            foreach (EventInfo evnt in model.GetType().GetEvents(flags))
            {
                if (!evnt.IsSpecialName && !evnt.DeclaringType.IsAssignableFrom(typeof(ModelCollectionFromResource)))
                {
                    DataRow row = table.NewRow();

                    row[0] = evnt.Name;
                    row[1] = evnt.EventHandlerType.Name;
                    row[2] = markdown.Transform(AutoDocumentation.GetSummary(evnt));
                    row[3] = markdown.Transform(AutoDocumentation.GetRemarks(evnt));

                    table.Rows.Add(row);
                }
            }

            return table;
        }

        private DataTable GetOutputs(IModel model)
        {
            DataTable table = new DataTable("Public Outputs");
            table.Columns.Add(new DataColumn("Name", typeof(string)));
            table.Columns.Add(new DataColumn("Units", typeof(string)));
            table.Columns.Add(new DataColumn("Type", typeof(string)));
            table.Columns.Add(new DataColumn("Description", typeof(string)));
            table.Columns.Add(new DataColumn("Remarks", typeof(string)));

            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
            foreach (PropertyInfo property in model.GetType().GetProperties(flags))
            {
                if (property.GetCustomAttribute<DescriptionAttribute>() == null &&
                    !property.DeclaringType.IsAssignableFrom(typeof(ModelCollectionFromResource)))
                {
                    DataRow row = table.NewRow();

                    row[0] = property.Name;
                    row[1] = property.GetCustomAttribute<UnitsAttribute>()?.ToString();
                    row[2] = property.PropertyType.Name;
                    row[3] = markdown.Transform(AutoDocumentation.GetSummary(property));
                    row[4] = markdown.Transform(AutoDocumentation.GetRemarks(property));

                    table.Rows.Add(row);
                }
            }

            return table;
        }

        private DataTable GetPublicMethods(IModel model)
        {
            DataTable table = new DataTable("Public Methods");
            table.Columns.Add(new DataColumn("Name", typeof(string)));
            table.Columns.Add(new DataColumn("Return type", typeof(string)));
            table.Columns.Add(new DataColumn("Description", typeof(string)));
            table.Columns.Add(new DataColumn("Remarks", typeof(string)));

            BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy;
            foreach (MethodInfo method in model.GetType().GetMethods(flags))
            {
                if (!method.IsSpecialName && !method.DeclaringType.IsAssignableFrom(typeof(ModelCollectionFromResource)))
                {
                    DataRow row = table.NewRow();

                    row[0] = method.Name;
                    row[1] = method.ReturnType.Name;
                    row[2] = markdown.Transform(AutoDocumentation.GetSummary(method));
                    row[3] = markdown.Transform(AutoDocumentation.GetRemarks(method));

                    table.Rows.Add(row);
                }
            }

            return table;
        }

        /// <summary>
        /// Get a table of all dependencies (Links) of a given model
        /// which match a given predicate.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="filter">Predicate on which to filter dependencies.</param>
        /// <returns></returns>
        private DataTable GetDependencies(IModel model, Predicate<MemberInfo> filter)
        {
            DataTable result = new DataTable("Functions");
            result.Columns.Add(new DataColumn("Name", typeof(string)));
            result.Columns.Add(new DataColumn("Type", typeof(string)));
            result.Columns.Add(new DataColumn("Link Type", typeof(string)));
            result.Columns.Add(new DataColumn("Link by Name", typeof(string)));
            result.Columns.Add(new DataColumn("Optional", typeof(string)));
            result.Columns.Add(new DataColumn("Path", typeof(string)));
            result.Columns.Add(new DataColumn("Description", typeof(string)));
            result.Columns.Add(new DataColumn("Remarks", typeof(string)));

            BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (MemberInfo member in model.GetType().GetMembers(flags))
            {
                LinkAttribute link = member.GetCustomAttribute<LinkAttribute>();
                if (link != null && filter(member))
                {
                    DataRow row = result.NewRow();
                    
                    row[0] = member.Name;
                    row[1] = GetMemberType(member).Name;
                    row[2] = link.Type.ToString();
                    row[3] = link.ByName.ToString();
                    row[4] = link.IsOptional.ToString();
                    row[5] = link.Path;
                    row[6] = markdown.Transform(AutoDocumentation.GetSummary(member));
                    row[7] = markdown.Transform(AutoDocumentation.GetRemarks(member));

                    result.Rows.Add(row);
                }
            }

            return result;
        }

        private Type GetMemberType(MemberInfo member)
        {
            if (member is PropertyInfo prop)
                return prop.PropertyType;

            if (member is FieldInfo field)
                return field.FieldType;

            if (member is MethodInfo)
                throw new NotImplementedException(); // return method.ReturnType;

            throw new Exception($"Unknown member type on member {member.Name} of type {member.DeclaringType.Name}: {member.GetType().Name}");
        }

        public void Detach()
        {
        }
    }
}
