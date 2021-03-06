﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Deployer.Core.Requirements;
using Deployer.Core.Services.Wim;
using Deployer.Gui.Properties;
using Deployer.Gui.Services;
using Deployer.Gui.ViewModels.Common;
using ReactiveUI;
using Serilog;
using Zafiro.Core.Files;
using Zafiro.Core.Patterns.Either;
using Zafiro.Core.UI;

namespace Deployer.Gui.ViewModels.RequirementSolvers
{
    public class WimPickRequirementSolver : ReactiveObject, IRequirementSolver
    {
        private readonly string key;
        private readonly DeployerFileOpenService fileOpenService;
        private readonly ObservableAsPropertyHelper<bool> hasWimHelper;
        private readonly IObservable<bool> isValid;
        private readonly ObservableAsPropertyHelper<WimMetadataViewModel> pickWimFileObs;
        
        public WimPickRequirementSolver(string key, Commands commands, DeployerFileOpenService fileOpenService)
        {
            this.key = key;
            this.fileOpenService = fileOpenService;
            OpenGetWoaCommand = commands.ShellOpen;
            PickWimFileCommand = ReactiveCommand
                .CreateFromObservable(() => PickWim()
                    .SelectMany(LoadWimMetadata)
                    .Select(x => x.Handle(errors => new WimMetadataViewModel(errors))));
            pickWimFileObs = PickWimFileCommand.ToProperty(this, x => x.WimMetadata);
            isValid = this.WhenAnyValue(model => model.WimMetadata, (WimMetadataViewModel x) => x != null && x.Errors == null);
            hasWimHelper = isValid
                .ToProperty(this, x => x.HasWim);
        }

        private IObservable<IZafiroFile> PickWim()
        {
            var filter = WimFilter();
            return fileOpenService.Picks(key, filter);
        }

        private static List<FileTypeFilter> WimFilter()
        {
            var filters = new List<FileTypeFilter>
            {
                new FileTypeFilter("Windows Images", "install.wim;install.esd"),
                new FileTypeFilter("Windows Images", "*.wim", "*.esd"),
                new FileTypeFilter("All files", "*.*"),
            };

            return filters;
        }

        public ReactiveCommand<Unit, WimMetadataViewModel> PickWimFileCommand { get; set; }
        public IReactiveCommand OpenGetWoaCommand { get; }
        public bool HasWim => hasWimHelper.Value;
        public WimMetadataViewModel WimMetadata => pickWimFileObs.Value;
        public virtual IObservable<bool> IsValid => isValid;

        public virtual IEnumerable<FulfilledRequirement> FulfilledRequirements()
        {
            return new[]
            {
                new FulfilledRequirement(key + "Path", WimMetadata.Path),
                new FulfilledRequirement(key + "Index", WimMetadata.SelectedDiskImage.Index),
            };
        }

        private async Task<Either<ErrorList, WimMetadataViewModel>> LoadWimMetadata(IZafiroFile file)
        {
            Log.Verbose("Trying to load WIM metadata file at '{ImagePath}'", file);

            using (var stream = await file.OpenForRead())
            {
                var imageReader = new WindowsImageMetadataReader();
                return imageReader
                    .Load(stream)
                    .MapRight(metadata =>
                    {
                        if (metadata.Images.All(x => x.Architecture != MyArchitecture.Arm64))
                        {
                            return new ErrorList(Resources.WimFileNoValidArchitecture);
                        }

                        var vm = new WimMetadataViewModel(metadata, file.Source.OriginalString);

                        Log.Verbose("WIM metadata file at '{ImagePath}' retrieved correctly",
                            file.Source.OriginalString);

                        return Either.Success<ErrorList, WimMetadataViewModel>(vm);
                    });
            }
        }
    }
}