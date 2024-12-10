﻿using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Injectio.Attributes;
using StabilityMatrix.Avalonia.Languages;
using StabilityMatrix.Avalonia.Models.Inference;
using StabilityMatrix.Avalonia.Services;
using StabilityMatrix.Avalonia.ViewModels.Base;
using StabilityMatrix.Avalonia.ViewModels.Dialogs;
using StabilityMatrix.Core.Attributes;
using StabilityMatrix.Core.Models.Api.Comfy.Nodes;

namespace StabilityMatrix.Avalonia.ViewModels.Inference.Modules;

[ManagedService]
[RegisterTransient<LoraModule>]
public partial class LoraModule : ModuleBase
{
    /// <inheritdoc />
    public override bool IsSettingsEnabled => true;

    /// <inheritdoc />
    public override IRelayCommand SettingsCommand => OpenSettingsDialogCommand;

    public LoraModule(ServiceManager<ViewModelBase> vmFactory)
        : base(vmFactory)
    {
        Title = "Lora";
        AddCards(
            vmFactory.Get<ExtraNetworkCardViewModel>(card =>
            {
                card.IsModelWeightEnabled = true;
                // Disable clip weight by default, but allow user to enable it
                card.IsClipWeightToggleEnabled = true;
                card.IsClipWeightEnabled = false;
            })
        );
    }

    [RelayCommand]
    private async Task OpenSettingsDialog()
    {
        var gridVm = VmFactory.Get<PropertyGridViewModel>(vm =>
        {
            vm.Title = $"{Title} {Resources.Label_Settings}";
            vm.SelectedObject = Cards.ToArray();
            vm.IncludeCategories = ["Settings"];
        });

        await gridVm.GetDialog().ShowAsync();
    }

    protected override void OnApplyStep(ModuleApplyStepEventArgs e)
    {
        var card = GetCard<ExtraNetworkCardViewModel>();

        // Skip if no lora model
        if (card.SelectedModel is not { } selectedLoraModel)
            return;

        // Add lora conditioning to all models
        foreach (var modelConnections in e.Builder.Connections.Models.Values)
        {
            if (modelConnections.Model is not { } model || modelConnections.Clip is not { } clip)
                continue;

            var loraLoader = e.Nodes.AddNamedNode(
                ComfyNodeBuilder.LoraLoader(
                    e.Nodes.GetUniqueName($"Loras_{modelConnections.Name}"),
                    model,
                    clip,
                    selectedLoraModel.RelativePath,
                    card.ModelWeight,
                    card.ClipWeight
                )
            );

            // Replace current model and clip with lora loaded model and clip
            modelConnections.Model = loraLoader.Output1;
            modelConnections.Clip = loraLoader.Output2;
        }
    }
}
