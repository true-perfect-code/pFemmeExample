using BlazorCore.Services.EventAggregator;
using System;
using System.Collections.Generic;
using System.Text;

namespace pFemmeExample.Shared.Services.EventAggregatorProject
{
    internal class EventAggregatorProjectModel
    {
    }

    public enum ModalNativeProjectType
    {
        CyclesCal,
        Cycle,
        AskAI,
        CyclesInfo,
        Synchronization
    }

    /// <summary>
    /// Event payload for native modal navigation.
    /// </summary>
    public sealed class ModalNativeProjectEventArgs
    {
        /// <summary>
        /// Which modal should be affected.
        /// </summary>
        public ModalNativeProjectType Modal { get; init; }

        /// <summary>
        /// True = open modal, False = close modal.
        /// </summary>
        public bool? IsOpen { get; init; }

        /// <summary>
        /// Optional payload for the modal.
        /// Example:
        /// Cycle model, DTO, navigation data, etc.
        /// </summary>
        public object? Data { get; init; }
    }
}
