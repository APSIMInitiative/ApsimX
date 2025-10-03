using Models;
using Models.Core;
using NUnit.Framework;
using System.IO;
using System.Linq;

namespace APSIM.Core.Tests;

[TestFixture]
public class CommandTests
{
    /*class ModelA : Model
    {
    }

    class ModelB : Model
    {
        public int B1 { get { return 3; } }
    }*/

    /// <summary>Ensure the add command works.</summary>
    [Test]
    public void EnsureAddWorks()
    {
        Simulation simulation = new();
        Node.Create(simulation);

        IModelCommand cmd = new AddCommand(modelReference: new NewModelReference("Report"),
                                           toPath: "[Simulation]",
                                           multiple: false);
        cmd.Run(simulation);

        Assert.That(simulation.Children[0], Is.InstanceOf(typeof(Models.Report)));
    }

    /// <summary>Ensure the add command works and that the child model is renamed.</summary>
    [Test]
    public void EnsureAddAndRenameWorks()
    {
        Simulation simulation = new();
        Node.Create(simulation);

        IModelCommand cmd = new AddCommand(modelReference: new NewModelReference("Report"),
                                           toPath: "[Simulation]",
                                           multiple: false,
                                           newName: "NewReport");

        cmd.Run(simulation);

        Assert.That(simulation.Children[0].Name, Is.EqualTo("NewReport"));
    }


    /// <summary>Ensure the add command works and that the child model is renamed.</summary>
    [Test]
    public void EnsureAddFromExtenalFileWorks()
    {
        // Create an external .apsimx file.
        Simulations simulations = new()
        {
            Children =
            [
                new Report() { Name = "NewReport" }
            ]
        };
        Node simulationsNode = Node.Create(simulations);
        string tempFilePath = Path.GetTempFileName();
        string json = FileFormat.WriteToString(simulationsNode);
        File.WriteAllText(tempFilePath, json);

        // Create a simulation that we will add a report model to.
        Simulation simulation = new();
        Node.Create(simulation);

        // Run add command to add report from external file into simulation.
        IModelCommand cmd = new AddCommand(modelReference: new ModelInFileReference(tempFilePath, "Report"),
                                           toPath: "[Simulation]",
                                           multiple: false,
                                           newName: "NewReport");
        cmd.Run(simulation);

        // Make sure report was added.
        Assert.That(simulation.Children[0].Name, Is.EqualTo("NewReport"));

        // Remove external file.
        File.Delete(tempFilePath);
    }
/*
    /// <summary>Ensure the delete command works.</summary>
    [Test]
    public void EnsureDeleteWorks()
    {
        Simulations simulation = new()
        {
            Children =
            [
                new Report() { Name = "NewReport" }
            ]
        };
        Node.Create(simulation);

        CommandProcessor commands = new([new DeleteCommand(modelName: "Report")]);

        commands.Run(simulation);

        Assert.That(simulation.Children.Count, Is.EqualTo(0));
    }

    /// <summary>Ensure the duplicate command works.</summary>
    [Test]
    public void EnsureDuplicateWorks()
    {
        Simulations simulation = new()
        {
            Children =
            [
                new Report() { Name = "Report" }
            ]
        };
        Node.Create(simulation);

        CommandProcessor commands = new([new DuplicateCommand(modelName: "Report", newName: "NewReport")]);

        commands.Run(simulation);

        Assert.That(simulation.Children.Count, Is.EqualTo(2));
        Assert.That(simulation.Children[1], Is.InstanceOf<Report>());
        Assert.That(simulation.Children[1].Name, Is.EqualTo("NewReport"));
    }

    /// <summary>Ensure the save command works.</summary>
    [Test]
    public void EnsureSaveWorks()
    {
        // Create a simulation in memory.
        Simulations simulations = new()
        {
            Children =
            [
                new Report() { Name = "Report" }
            ]
        };
        Node.Create(simulations);

        // Run the save command.
        string tempFilePath = Path.GetTempFileName();
        CommandProcessor commands = new([new SaveCommand(fileName: tempFilePath)]);
        commands.Run(simulations);

        Assert.That(File.Exists(tempFilePath));
        Node simulationsReadIn = FileFormat.ReadFromFile<Simulations>(tempFilePath);
        Assert.That(simulationsReadIn.Children.First().Name, Is.EqualTo("Report"));
    }

    /// <summary>Ensure the set properties command works.</summary>
    [Test]
    public void EnsureSetPropertiesWorks()
    {
        Simulations simulation = new()
        {
            Children =
            [
                new Clock()
                {
                    StartDate = new(1900, 1, 1),
                    EndDate = new(2000, 1, 1)
                }
            ]
        };
        Node.Create(simulation);

        (string name, string value)[] properties =
        [
            ("[Clock].StartDate", "2000-01-01"),
            ("[Clock].EndDate", "2000-12-31")
        ];

        CommandProcessor commands = new([new SetPropertiesCommand(properties)]);
        commands.Run(simulation);

        var clock = simulation.Children.First() as Clock;
        Assert.That(clock.StartDate, Is.EqualTo(new System.DateTime(2000, 1, 1)));
        Assert.That(clock.EndDate, Is.EqualTo(new System.DateTime(2000, 12, 31)));
    }*/
}
