using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models;
using UserInterface.Views;
using System.Reflection;

namespace UserInterface.Presenters
{
    class ReportPresenter : IPresenter
    {
        private Report Report;
        private IReportView View;
        private CommandHistory CommandHistory;

        /// <summary>
        /// Attach the model (report) and the view (IReportView)
        /// </summary>
        public void Attach(object Model, object View, CommandHistory CommandHistory)
        {
            this.Report = Model as Report;
            this.CommandHistory = CommandHistory;
            this.View = View as IReportView;

            this.View.VariableList.Lines = Report.Variables;
            this.View.EventList.Lines = Report.Events;
            this.View.VariableList.ContextItemsNeeded += OnNeedVariableNames;
            this.View.EventList.ContextItemsNeeded += OnNeedEventNames;
            this.View.VariableList.TextHasChangedByUser += OnVariableNamesChanged;
            this.View.EventList.TextHasChangedByUser += OnEventNamesChanged;
            CommandHistory.ModelChanged += CommandHistory_ModelChanged;
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            this.View.VariableList.ContextItemsNeeded -= OnNeedVariableNames;
            this.View.EventList.ContextItemsNeeded -= OnNeedEventNames;
            this.View.VariableList.TextHasChangedByUser -= OnVariableNamesChanged;
            this.View.EventList.TextHasChangedByUser -= OnEventNamesChanged;
            CommandHistory.ModelChanged -= CommandHistory_ModelChanged;
        }

        /// <summary>
        /// The view is asking for variable names.
        /// </summary>
        void OnNeedVariableNames(object Sender, Utility.NeedContextItems e)
        {
            if (e.ObjectName == "")
                e.ObjectName = ".";
            object o = Report.ParentZone.Get(e.ObjectName);

            if (o != null)
            {
                foreach (PropertyInfo Property in o.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (Property.Name == "Models")
                    {
                        List<object> Models = Property.GetValue(o, null) as List<object>;
                        if (Models != null)
                        {
                            foreach (object Model in Models)
                            {
                                e.Items.Add(Utility.Reflection.Name(Model));
                            }
                        }
                    }
                    else
                        e.Items.Add(Property.Name);
                }
            }
        }

        /// <summary>
        /// The view is asking for event names.
        /// </summary>
        void OnNeedEventNames(object Sender, Utility.NeedContextItems e)
        {
            object o = Report.ParentZone.Get(e.ObjectName);

            if (o != null)
            {
                foreach (EventInfo Event in o.GetType().GetEvents(BindingFlags.Instance | BindingFlags.Public))
                    e.Items.Add(Event.Name);
            }
        }

        /// <summary>
        /// The variable names have changed in the view.
        /// </summary>
        void OnVariableNamesChanged(object sender, EventArgs e)
        {
            CommandHistory.ModelChanged -= new CommandHistory.ModelChangedDelegate(CommandHistory_ModelChanged);
            CommandHistory.Add(new Commands.ChangePropertyCommand(Report, "Variables", View.VariableList.Lines));
            CommandHistory.ModelChanged += new CommandHistory.ModelChangedDelegate(CommandHistory_ModelChanged);
        }

        /// <summary>
        /// The event names have changed in the view.
        /// </summary>
        void OnEventNamesChanged(object sender, EventArgs e)
        {
            CommandHistory.ModelChanged -= new CommandHistory.ModelChangedDelegate(CommandHistory_ModelChanged);
            CommandHistory.Add(new Commands.ChangePropertyCommand(Report, "Events", View.EventList.Lines));
            CommandHistory.ModelChanged += new CommandHistory.ModelChangedDelegate(CommandHistory_ModelChanged);
        }

        /// <summary>
        /// The model has changed so update our view.
        /// </summary>
        void CommandHistory_ModelChanged(object changedModel)
        {
            if (changedModel == Report)
            {
                View.VariableList.Lines = Report.Variables;
                View.EventList.Lines = Report.Events;
            }
        }


    }
}
