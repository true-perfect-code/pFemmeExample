using BlazorCore.Services.EventAggregator;
using pFemmeExample.Shared.Global;
using System;
using System.Collections.Generic;
using System.Text;

namespace pFemmeExample.Shared.Services.EventAggregatorProject
{
    public class EventAggregatorProject : IEventAggregatorProject
    {
        // === UI Events ===

        /// <inheritdoc />
        public event Func<Task>? OnRefreshingData;
        public async Task RefreshingData()
        {
            if (OnRefreshingData != null)
            {
                await OnRefreshingData.Invoke();
            }
        }

        /// <inheritdoc />
        public event Func<Task>? OnRefreshingCyclesCal;
        public async Task RefreshingCyclesCal()
        {
            if (OnRefreshingCyclesCal != null)
            {
                await OnRefreshingCyclesCal.Invoke();
            }
        }


        // === Navigation Modal-Native ===

        public event Func<ModalNativeProjectEventArgs, Task>? OnModalNativeChanged;

        /// <summary>
        /// Opens or closes a native modal and dispatches the event to all subscribers.
        /// </summary>
        public async Task ChangeModalNativeAsync(
            ModalNativeProjectType modal,
            bool? isOpen = null,
            object? data = null)
        {
            // No subscribers
            if (OnModalNativeChanged is null)
                return;

            // Create event payload
            var args = new ModalNativeProjectEventArgs
            {
                Modal = modal,
                IsOpen = isOpen,
                Data = data
            };

            // Execute all subscribers safely
            foreach (Func<ModalNativeProjectEventArgs, Task> handler
                     in OnModalNativeChanged.GetInvocationList())
            {
                try
                {
                    await handler(args);
                }
                catch (Exception ex)
                {
                    // Optional:
                    // Replace with your logging system
                    Console.WriteLine(
                        $"[EventAggregatorProject] Modal event error: {ex}");
                }
            }
        }

        /// <inheritdoc />
        public event Func<Task>? OnFireAfterCycleData;
        public async Task FireAfterCycleData()
        {
            if (OnFireAfterCycleData != null)
            {
                await OnFireAfterCycleData.Invoke();
            }
        }
    }
}
