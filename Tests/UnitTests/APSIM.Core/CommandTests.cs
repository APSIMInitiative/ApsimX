using Atk;
using Models;
using Models.Climate;
using Models.Core;
using Models.PMF;
using NUnit.Framework;
using System.IO;
using System.Linq;
using UnitTests.Core.Run;

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
        cmd.Run(simulation, runner: null);

        Assert.That(simulation.Children[0], Is.InstanceOf(typeof(Models.Report)));
    }

    /// <summary>Ensure the add command works when multiple=true</summary>
    [Test]
    public void EnsureAddMultipleWorks()
    {
        var simulations = new Simulations()
        {
            Children = [
                new Simulation(),
                new Simulation()
            ]
        };
        Node.Create(simulations);

        IModelCommand cmd = new AddCommand(modelReference: new NewModelReference("Report"),
                                           toPath: "[Simulation]",
                                           multiple: true);
        cmd.Run(simulations, runner: null);

        Assert.That(simulations.Children[0].Children[0], Is.InstanceOf(typeof(Models.Report)));
        Assert.That(simulations.Children[1].Children[0], Is.InstanceOf(typeof(Models.Report)));
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

        cmd.Run(simulation, runner: null);

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
        cmd.Run(simulation, runner: null);

        // Make sure report was added.
        Assert.That(simulation.Children[0].Name, Is.EqualTo("NewReport"));

        // Remove external file.
        File.Delete(tempFilePath);
    }

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

        IModelCommand cmd = new DeleteCommand(modelName: "Report");
        cmd.Run(simulation, runner: null);

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

        IModelCommand cmd = new DuplicateCommand(modelName: "Report", newName: "NewReport");
        cmd.Run(simulation, runner: null);

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
        IModelCommand cmd = new SaveCommand(fileName: tempFilePath);
        var saveModel = cmd.Run(simulations, runner: null);

        Assert.That(File.Exists(tempFilePath));
        Node simulationsReadIn = FileFormat.ReadFromFile<Simulations>(tempFilePath);
        Assert.That(simulationsReadIn.Children.First().Name, Is.EqualTo("Report"));

        // Ensure the save command also changed Node.FileName
        Assert.That(saveModel.Node.FileName, Is.EqualTo(tempFilePath));
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

        IModelCommand cmd = new SetPropertyCommand("[Clock].StartDate", "=", "2000-01-01", fileName: null);
        cmd.Run(simulation, runner: null);

        var clock = simulation.Children.First() as Clock;
        Assert.That(clock.StartDate, Is.EqualTo(new System.DateTime(2000, 1, 1)));
    }

    /// <summary>Ensure we can undo a set properties command.</summary>
    [Test]
    public void EnsureUndoSetPropertiesWorks()
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

        IModelCommand cmd = new SetPropertyCommand("[Clock].StartDate", "=", "2000-01-01", fileName: null);
        cmd.Run(simulation, runner: null);

        var clock = simulation.Children.First() as Clock;
        Assert.That(clock.StartDate, Is.EqualTo(new System.DateTime(2000, 1, 1)));

        // Now undo the command.
        (cmd as SetPropertyCommand).Undo();
        Assert.That(clock.StartDate, Is.EqualTo(new System.DateTime(1900, 1, 1)));
    }

    /// <summary>Ensure can set a string property to "".</summary>
    [Test]
    public void EnsureSetPropertyToEmpty()
    {
        Simulations simulation = new()
        {
            Children =
            [
                new Weather()
                {
                    FileName = "file1.apsimx"
                }
            ]
        };
        Node.Create(simulation);

        IModelCommand cmd = new SetPropertyCommand("[Weather].FileName", "=", "", fileName: null);
        cmd.Run(simulation, runner: null);

        var weather = simulation.Children.First() as Weather;
        Assert.That(weather.FileName, Is.Empty);
    }

    /// <summary>Ensure the set property += command works.</summary>
    [Test]
    public void EnsureSetPropertyAddToArrayWorks()
    {
        Simulations simulation = new()
        {
            Children =
            [
                new Cultivar()
                {
                    Command = [ "a=1" ]
                }
            ]
        };
        Node.Create(simulation);

        IModelCommand cmd = new SetPropertyCommand("[Cultivar].Command", "+=", "b=2", fileName: null);
        cmd.Run(simulation, runner: null);

        var cultivar = simulation.Children.First() as Cultivar;
        Assert.That(cultivar.Command, Is.EqualTo(["a=1", "b=2"]));
    }

    /// <summary>Ensure the set property += command overwrites existing value when it exists.</summary>
    [Test]
    public void EnsureSetPropertyAddOverwritesExisting()
    {
        Simulations simulation = new()
        {
            Children =
            [
                new Cultivar()
                {
                    Command = [ "a=1" ]
                }
            ]
        };
        Node.Create(simulation);

        IModelCommand cmd = new SetPropertyCommand("[Cultivar].Command", "+=", "a=2", fileName: null);
        cmd.Run(simulation, runner: null);

        var cultivar = simulation.Children.First() as Cultivar;
        Assert.That(cultivar.Command, Is.EqualTo(["a=2"]));
    }

    /// <summary>Ensure the set array property works.</summary>
    [Test]
    public void EnsureSetArrayPropertyWorks()
    {
        Simulations simulation = new()
        {
            Children =
            [
                new Cultivar()
                {
                    Command = [ "a=1" ]
                }
            ]
        };
        Node.Create(simulation);

        IModelCommand cmd = new SetPropertyCommand("[Cultivar].Command", "=", "b=2,c=3", fileName: null);
        cmd.Run(simulation, runner: null);

        var cultivar = simulation.Children.First() as Cultivar;
        Assert.That(cultivar.Command, Is.EqualTo(["b=2", "c=3"]));
    }

    /// <summary>Ensure the set array property to empty string clears the array.</summary>
    [Test]
    public void EnsureSetArrayPropertyToEmptyWorks()
    {
        Simulations simulation = new()
        {
            Children =
            [
                new Cultivar()
                {
                    Command = [ "a=1" ]
                }
            ]
        };
        Node.Create(simulation);

        IModelCommand cmd = new SetPropertyCommand("[Cultivar].Command", "=", "", fileName: null);
        cmd.Run(simulation, runner: null);

        var cultivar = simulation.Children.First() as Cultivar;
        Assert.That(cultivar.Command, Is.Empty);
    }

    /// <summary>Ensure the set array property to null sets the array to null.</summary>
    [Test]
    public void EnsureSetArrayPropertyToNullWorks()
    {
        Simulations simulation = new()
        {
            Children =
            [
                new Cultivar()
                {
                    Command = [ "a=1" ]
                }
            ]
        };
        Node.Create(simulation);

        IModelCommand cmd = new SetPropertyCommand("[Cultivar].Command", "=", "null", fileName: null);
        cmd.Run(simulation, runner: null);

        var cultivar = simulation.Children.First() as Cultivar;
        Assert.That(cultivar.Command, Is.Null);
    }

    /// <summary>Ensure the set array element property works.</summary>
    [Test]
    public void EnsureSetArrayElementWorks()
    {
        Simulations simulation = new()
        {
            Children =
            [
                new Cultivar()
                {
                    Command = [ "a=1", "b=2" ]
                }
            ]
        };
        Node.Create(simulation);

        IModelCommand cmd = new SetPropertyCommand("[Cultivar].Command[1]", "=", "b=1", fileName: null);
        cmd.Run(simulation, runner: null);

        var cultivar = simulation.Children.First() as Cultivar;
        Assert.That(cultivar.Command, Is.EqualTo(["b=1", "b=2"]));
    }


    /// <summary>Ensure the set array to an empty array works.</summary>
    [Test]
    public void EnsureSetArrayToEmptyWorks()
    {
        Simulations simulation = new()
        {
            Children =
            [
                new Cultivar()
                {
                    Command = [ "a", "b" ]
                }
            ]
        };
        Node.Create(simulation);

        IModelCommand cmd = new SetPropertyCommand("[Cultivar].Command", "=", "", fileName: null);
        cmd.Run(simulation, runner: null);

        var cultivar = simulation.Children.First() as Cultivar;
        Assert.That(cultivar.Command.Length, Is.EqualTo(0));
    }

    /// <summary>Ensure the set property -= command works.</summary>
    [Test]
    public void EnsureSetPropertyDeleteFromArrayWorks()
    {
        Simulations simulation = new()
        {
            Children =
            [
                new Cultivar()
                {
                    Command = [ "a=1", "b=2" ]
                }
            ]
        };
        Node.Create(simulation);

        IModelCommand cmd = new SetPropertyCommand("[Cultivar].Command", "-=", "b", fileName: null);
        cmd.Run(simulation, runner: null);

        var cultivar = simulation.Children.First() as Cultivar;
        Assert.That(cultivar.Command, Is.EqualTo(["a=1"]));
    }


    /// <summary>Ensure the set property -= command doesn't throw when element to delete cannot be found.</summary>
    [Test]
    public void EnsureSetPropertyDeleteDoesntThrowWhenMissingElement()
    {
        Simulations simulation = new()
        {
            Children =
            [
                new Cultivar()
                {
                    Command = [ "a=1", "b=2" ]
                }
            ]
        };
        Node.Create(simulation);

        IModelCommand cmd = new SetPropertyCommand("[Cultivar].Command", "-=", "c", fileName: null);
        cmd.Run(simulation, runner: null);

        var cultivar = simulation.Children.First() as Cultivar;
        Assert.That(cultivar.Command, Is.EqualTo(["a=1", "b=2"]));
    }

    /// <summary>Ensure the load command works.</summary>
    [Test]
    public void EnsureLoadWorks()
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
        IModelCommand saveCommand = new SaveCommand(fileName: tempFilePath);
        saveCommand.Run(relativeTo: simulations, runner: null);

        // Run load command. It should return a relativeTo that is from the temp file.
        IModelCommand loadCommand = new LoadCommand(tempFilePath);
        var relativeTo = loadCommand.Run(relativeTo: null, runner: null);

        Assert.That(relativeTo.GetChildren().First().Name, Is.EqualTo("Report"));
    }

    /// <summary>Ensure the load command works.</summary>
    [Test]
    public void EnsureRunWorks()
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

        MockRunner mockRunner = new();

        // Run the run command.
        IModelCommand cmd = new RunCommand();
        cmd.Run(simulations, runner: mockRunner);

        Assert.That(mockRunner.RunCalled, Is.True);
        Assert.That(mockRunner.RelativeTo, Is.EqualTo(simulations));
    }
}
