﻿using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MudBlazor
{
    public partial class MudAutocomplete<T> : MudBaseInput<T>, IDisposable
    {

        protected string Classname =>
            new CssBuilder("mud-select")
            .AddClass(Class)
            .Build();

        /// <summary>
        /// If true, compact vertical padding will be applied to all select items.
        /// </summary>
        [Parameter] public bool Dense { get; set; }

        /// <summary>
        /// The Open Select Icon
        /// </summary>
        [Parameter] public string OpenIcon { get; set; } = Icons.Material.ArrowDropUp;

        /// <summary>
        /// The Open Select Icon
        /// </summary>
        [Parameter] public string CloseIcon { get; set; } = Icons.Material.ArrowDropDown;

        internal event Action<HashSet<T>> SelectionChangedFromOutside;

        /// <summary>
        /// Sets the maxheight the select can have when open.
        /// </summary>
        [Parameter] public int MaxHeight { get; set; } = 300;

        /// <summary>
        /// Defines how values are displayed in the drop-down list
        /// </summary>
        [Parameter]
        public Expression<Func<T, object>> ToStringExpression{ get; set; } = (x) => x;

        /// <summary>
        /// The SearchFunc returns a list of items matching the typed text
        /// </summary>
        [Parameter]
        public Func<string, Task<IEnumerable<T>>> SearchFunc { get; set; }

        /// <summary>
        /// Set the format for values in the list if no template
        /// </summary>
        [Parameter]
        public string Format { get; set; }

        /// <summary>
        /// Maximum items to display, defaults to 10.
        /// Set null to display all
        /// </summary>
        [Parameter]
        public int? MaxItems { get; set; } = 10;

        /// <summary>
        /// Minimum characters to initiate a search, defaults to 2
        /// </summary>
        [Parameter]
        public int MinCharacters { get; set; } = 0;

        /// <summary>
        /// Debounce interval in milliseconds.
        /// </summary>
        [Parameter] public int DebounceInterval { get; set; } = 100;


        internal bool IsOpen { get; set; }

        public string CurrentIcon { get; set; }

        public void SelectOption(T value)
        {
            Value = value;
            if (Items!=null)
                SelectedListItemIndex = Array.IndexOf(Items, value);
            _text = GetItemString(value);
            Timer?.Dispose();
            IsOpen = false;
            UpdateIcon();
            
            StateHasChanged();
        }

        public void ToggleMenu()
        {
            if (Disabled)
                return;
            IsOpen = !IsOpen;
            if (IsOpen && string.IsNullOrEmpty(Text))
                IsOpen = false;
            UpdateIcon();
            StateHasChanged();
        }

        public void UpdateIcon()
        {
            if (IsOpen)
            {
                CurrentIcon = OpenIcon;
            }
            else
            {
                CurrentIcon = CloseIcon;
            }
        }

        protected override void OnInitialized()
        {
            UpdateIcon();
        }

        private Timer Timer;
        private T[] Items;
        private int SelectedListItemIndex = 0;

        /// <summary>
        /// The user typed something ...
        /// </summary>
        /// <param name="text"></param>
        protected override void StringValueChanged(string text)
        {
            Timer?.Dispose();
            //Text = GetValue((T)text.Value);
            var autoReset = new AutoResetEvent(false);
            Timer = new Timer(OnTimerComplete, autoReset, DebounceInterval, Timeout.Infinite);
        }

        private void OnTimerComplete(object stateInfo) => InvokeAsync(async () =>
        {
            if (!string.IsNullOrWhiteSpace(Text) && Text.Length >= MinCharacters)
            {
                SelectedListItemIndex = 0;

                var searched_items = await SearchFunc(Text);
                if (MaxItems.HasValue)
                    searched_items = searched_items.Take(MaxItems.Value);
                Items = searched_items.ToArray();

                if (Items?.Count() == 0)
                {
                    IsOpen = false;
                    UpdateIcon();
                    StateHasChanged();
                    return;
                }

                IsOpen = true;
                UpdateIcon();
                StateHasChanged();
            }
        });

        private Func<T, object> toStringFunc;

        private string GetItemString(T item)
        {
            if (item == null) return string.Empty;

            if (toStringFunc == null)
                toStringFunc = ToStringExpression.Compile();

            object value = null;

            try
            {
                value = toStringFunc.Invoke(item);
            }
            catch (NullReferenceException) { }

            if (value == null) return string.Empty;

            if (string.IsNullOrEmpty(Format))
                return value.ToString();

            return string.Format(CultureInfo.CurrentCulture, $"{{0:{Format}}}", value);
        }

        protected virtual void OnInputKeyDown(KeyboardEventArgs args)
        {
            switch (args.Key)
            {
                case "Enter":
                    OnEnterKey();
                    break;
                case "ArrowDown":
                    SelectNextItem(+1);
                    break;
                case "ArrowUp":
                    SelectNextItem(-1);
                    break;
            }
        }

        private void SelectNextItem(int increment)
        {
            if (Items == null || Items.Length == 0)
                return;
            SelectedListItemIndex = Math.Max(0,  Math.Min(Items.Length-1, SelectedListItemIndex+ increment));
            // TODO: scroll the list to the currently selected item!!!
            StateHasChanged();
        }

        private void OnEnterKey()
        {
            if (IsOpen == false)
                return;
            if (Items == null || Items.Length == 0)
                return;
            if (SelectedListItemIndex >= 0 && SelectedListItemIndex < Items.Length)
                SelectOption(Items[SelectedListItemIndex]);
        }

        protected override void Dispose(bool disposing)
        {
            Timer?.Dispose();
            base.Dispose(disposing);
        }
    }
}
