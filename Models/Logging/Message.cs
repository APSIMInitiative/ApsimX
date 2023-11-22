using System;
using System.Text;
using Models.Core;

namespace Models.Logging
{
    /// <summary>
    /// Encapsulates a message written to the simulation log.
    /// </summary>
    public class Message
    {
        /// <summary>
        /// The date on which the message was sent.
        /// </summary>
        public DateTime Date { get; private set; }

        /// <summary>
        /// The contents of the message.
        /// </summary>
        public string Text { get; private set; }

        /// <summary>
        /// The model sending the message.
        /// </summary>
        public IModel Provider { get; private set; }

        /// <summary>The severity/type of the message.</summary>
        public MessageType Severity { get; private set; }

        /// <summary>
        /// Name of the simulation in which the messsage was sent.
        /// </summary>
        public string SimulationName { get; private set; }

        /// <summary>
        /// Name of the zone in which the message was sent.
        /// </summary>
        public string Zone => Provider.FindAncestor<Zone>()?.Name;

        /// <summary>
        /// Relative path - used for export functionality.
        /// </summary>
        public string RelativePath { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="date">The date on which the message was sent.</param>
        /// <param name="text">The contents of the message.</param>
        /// <param name="sender">The sender of the message.</param>
        /// <param name="severity">The severity of the message.</param>
        /// <param name="simulationName">Name of the simulation in which the message was sent.</param>
        /// <param name="senderPath">Relative path of the sender model.</param>
        public Message(DateTime date, string text, IModel sender, MessageType severity, string simulationName, string senderPath)
        {
            Date = date;
            Text = text;
            Provider = sender;
            Severity = severity;
            SimulationName = simulationName;
            RelativePath = senderPath;
        }

        /// <summary>
        /// Export the message to a markdown format.
        /// </summary>
        public string ToMarkdown()
        {
            StringBuilder markdown = new StringBuilder();
            markdown.AppendLine($"### {Date:yyyy-MM-dd} {RelativePath}");
            markdown.AppendLine();
            markdown.AppendLine("```");
            markdown.AppendLine(Text);
            markdown.AppendLine("```");
            markdown.AppendLine();

            return markdown.ToString();
        }
    }
}