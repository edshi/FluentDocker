﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ductus.FluentDocker.Extensions;
using Ductus.FluentDocker.Model.Builders;
using Ductus.FluentDocker.Model.Common;
using Ductus.FluentDocker.Services;

namespace Ductus.FluentDocker.Builders
{
  public sealed class FileBuilder
  {
    private readonly FileBuilderConfig _config = new FileBuilderConfig();
    private readonly ImageBuilder _parent;
    private TemplateString _workingFolder;

    internal FileBuilder(ImageBuilder parent)
    {
      _parent = parent;
    }

    internal string PrepareBuild()
    {
      if (null == _workingFolder)
      {
        _workingFolder = @"${TEMP}/fluentdockertest/${RND}";
      }

      CopyToWorkDir(_workingFolder); // Must be before RenderDockerFile!
      RenderDockerfile(_workingFolder);
      return _workingFolder;
    }

    public IContainerImageService Build()
    {
      return _parent.Build();
    }

    public Builder Builder()
    {
      return _parent.Builder();
    }

    public ImageBuilder ToImage()
    {
      return _parent;
    }

    public FileBuilder WorkingFolder(TemplateString workingFolder)
    {
      _workingFolder = workingFolder;
      return this;
    }

    public FileBuilder UseParent(string from)
    {
      _config.From = from;
      return this;
    }

    public FileBuilder Maintainer(string maintainer)
    {
      _config.Maintainer = maintainer;
      return this;
    }

    public FileBuilder Add(TemplateString source, TemplateString destination)
    {
      _config.AddRunCommands.Add(new AddCommand {Source = source, Destination = destination});
      return this;
    }

    public FileBuilder Run(params TemplateString[] commands)
    {
      _config.AddRunCommands.Add(new RunCommand {Lines = new List<TemplateString>(commands)});
      return this;
    }

    public FileBuilder UseWorkDir(string workdir)
    {
      _config.Workdir = workdir;
      return this;
    }

    public FileBuilder ExposePorts(params int[] ports)
    {
      _config.Expose = new List<int>(ports);
      return this;
    }

    public FileBuilder Command(string command, params string[] args)
    {
      _config.Command.Add(command);
      if (null != args && 0 != args.Length)
      {
        ((List<string>) _config.Command).AddRange(args);
      }
      return this;
    }

    public FileBuilder FromFile(string file)
    {
      _config.UseFile = file;
      return this;
    }

    public FileBuilder FromString(string dockerFileAsString)
    {
      _config.DockerFileString = dockerFileAsString;
      return this;
    }

    /// <summary>
    ///   Copies all files and folders to working directory.
    ///   Note that this method will mutate <see cref="AddCommand.Source" /> to
    ///   a relative path!!
    /// </summary>
    /// <param name="workingFolder">The working folder.</param>
    private void CopyToWorkDir(TemplateString workingFolder)
    {
      foreach (var command in _config.AddRunCommands.Where(x => x is AddCommand).Cast<AddCommand>())
      {
        var wff = Path.Combine(workingFolder, command.Source);
        if (File.Exists(wff) || Directory.Exists(wff))
        {
          // File or folder already at working folder - no need to copy.
          continue;
        }

        // Replace to relative path
        command.Source = command.Source.Copy(workingFolder);
      }
    }

    private void RenderDockerfile(TemplateString workingFolder)
    {
      if (Directory.Exists(workingFolder))
      {
        Directory.CreateDirectory(workingFolder);
      }

      var dockerFile = Path.Combine(workingFolder, "Dockerfile");

      string contents = null;
      if (!string.IsNullOrEmpty(_config.UseFile))
      {
        contents = _config.UseFile.FromFile();
      }

      if (!string.IsNullOrWhiteSpace(_config.DockerFileString))
      {
        contents = _config.DockerFileString;
      }

      if (string.IsNullOrEmpty(contents))
      {
        contents = _config.ToString();
      }

      contents.ToFile(dockerFile);
    }
  }
}