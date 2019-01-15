using ApsimNG.Views.CLEM;
using Models.Core;
using Models.CLEM.Reporting;
using System;
using UserInterface.Commands;
using UserInterface.Presenters;

namespace ApsimNG.Presenters.CLEM
{
    class CustomQueryPresenter : IPresenter
    {
        private CustomQuery query = null;
        private CustomQueryView view = null;
        private ExplorerPresenter explorer = null;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        /// <param name="view"></param>
        /// <param name="explorerPresenter"></param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.query = model as CustomQuery;
            this.view = view as CustomQueryView;
            this.explorer = explorerPresenter;

            this.view.OnRunQuery += RunQuery;
            this.view.OnLoadFile += LoadFile;

            // If the model contains sql, update the view to match it
            if (!string.IsNullOrEmpty(query.Sql))
            {
                this.view.Sql = query.Sql;
                this.view.Filename = query.Filename;
                RunQuery(this, EventArgs.Empty);
            }

            // If the model contains a filenme, update the view to match it
            if (!string.IsNullOrEmpty(query.Filename))
            {
                this.view.Filename = query.Filename;

            }
        }        

        /// <summary>
        /// 
        /// </summary>
        public void Detach()
        {
            view.OnRunQuery -= RunQuery;
            view.OnLoadFile -= LoadFile;
            view.gridview1.Dispose();
            view.Detach();
            SaveData();
        }

        private void SaveData()
        {
            ChangeProperty sqlcom = new ChangeProperty(this.query, "Sql", view.Sql);
            explorer.CommandHistory.Add(sqlcom);

            ChangeProperty filecom = new ChangeProperty(this.query, "Filename", view.Filename);
            explorer.CommandHistory.Add(filecom);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RunQuery(object sender, EventArgs e)
        {
            IStorageReader reader = Apsim.Find(query, typeof(IStorageReader)) as IStorageReader;
            view.gridview1.DataSource = reader.RunQuery(view.Sql);

            SaveData();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoadFile(object sender, EventArgs e)
        {
            SaveData();
        }
    }
}
