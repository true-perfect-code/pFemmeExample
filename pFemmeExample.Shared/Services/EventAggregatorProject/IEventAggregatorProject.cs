using BlazorCore.Services.EventAggregator;
using System;
using System.Collections.Generic;
using System.Text;

namespace pFemmeExample.Shared.Services.EventAggregatorProject
{
    public interface IEventAggregatorProject
    {
        // === UI Events ===

        /// <summary>
        /// Triggered when the data needs to be refreshed.
        /// </summary>
        event Func<Task>? OnRefreshingData;
        Task RefreshingData();

        /// <summary>
        /// Triggered refreshing data.
        /// </summary>
        event Func<Task>? OnRefreshingCyclesCal;
        Task RefreshingCyclesCal();


        // === Navigation Modal-Native ===

        /// <summary>
        /// Triggered when a native modal should open or close.
        /// </summary>
        event Func<ModalNativeProjectEventArgs, Task>? OnModalNativeChanged;

        /// <summary>
        /// Opens or closes a native modal.
        /// </summary>
        /// <param name="modal">Target modal type.</param>
        /// <param name="isOpen">Optional True = open, False = close.</param>
        /// <param name="data">Optional payload.</param>
        Task ChangeModalNativeAsync(
            ModalNativeProjectType modal,
            bool? isOpen = null,
            object? data = null);


        /// <summary>
        /// Triggered when close Cycle to refresh trends.
        /// </summary>
        event Func<Task>? OnFireAfterCycleData;
        Task FireAfterCycleData();

    }
}
