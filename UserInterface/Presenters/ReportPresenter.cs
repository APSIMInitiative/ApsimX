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

            this.View.VariableNames = Report.Variables;
            this.View.EventNames = Report.Events;
            this.View.NeedVariableNames += OnNeedVariableNames;
            this.View.NeedEventNames += OnNeedEventNames;
            this.View.VariableNamesChanged +=OnVariableNamesChanged;
            this.View.EventNamesChanged += OnEventNamesChanged;
            CommandHistory.ModelChanged += CommandHistory_ModelChanged;
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            View.NeedVariableNames -= OnNeedVariableNames;
            View.NeedEventNames -= OnNeedEventNames;
            View.VariableNamesChanged -= OnVariableNamesChanged;
            View.EventNamesChanged -= OnEventNamesChanged;
            CommandHistory.ModelChanged -= CommandHistory_ModelChanged;
        }

        /// <summary>
        /// The view is asking for variable names.
        /// </summary>
        void OnNeedVariableNames(object Sender, Utility.Editor.NeedContextItems e)
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
        void OnNeedEventNames(object Sender, Utility.Editor.NeedContextItems e)
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
            CommandHistory.Add(new Commands.ChangePropertyCommand(Report, "Variables", View.VariableNames));
            CommandHistory.ModelChanged += new CommandHistory.ModelChangedDelegate(CommandHistory_ModelChanged);
        }

        /// <summary>
        /// The event names have changed in the view.
        /// </summary>
        void OnEventNamesChanged(object sender, EventArgs e)
        {
            CommandHistory.ModelChanged -= new CommandHistory.ModelChangedDelegate(CommandHistory_ModelChanged);
            CommandHistory.Add(new Commands.ChangePropertyCommand(Report, "Events", View.EventNames));
            CommandHistory.ModelChanged += new CommandHistory.ModelChangedDelegate(CommandHistory_ModelChanged);
        }

        /// <summary>
        /// The model has changed so update our view.
        /// </summary>
        void CommandHistory_ModelChanged(object changedModel)
        {
            if (changedModel == Report)
            {
                View.VariableNames = Report.Variables;
                View.EventNames = Report.Events;
            }
        }


    }
}
