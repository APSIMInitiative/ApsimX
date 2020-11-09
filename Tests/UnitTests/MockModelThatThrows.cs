namespace UnitTests
{
    using Models.Core;
    using System;

    [Serializable]
    class MockModelThatThrows : Model
    {
        /// <summary>An event handler to signal start of a simulation.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            throw new Exception("Intentional exception");
        }
    }
}
