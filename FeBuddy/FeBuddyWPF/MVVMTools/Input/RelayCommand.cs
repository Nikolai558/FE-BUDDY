// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable SA1512

// This file is inspired from the MvvmLight library (lbugnion/MvvmLight),
// more info in ThirdPartyNotices.txt in the root of the project.

// This file is grabbed from CommunityToolkit.Mvvm.Input

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FeBuddyWPF.MVVMTools.Input
{
    /// <summary>
    /// An interface expanding <see cref="ICommand"/> with the ability to raise
    /// the <see cref="ICommand.CanExecuteChanged"/> event externally.
    /// </summary>
    public interface IRelayCommand : ICommand
    {
        /// <summary>
        /// Notifies that the <see cref="ICommand.CanExecute"/> property has changed.
        /// </summary>
        void NotifyCanExecuteChanged();
    }

    /// <summary>
    /// A command whose sole purpose is to relay its functionality to other
    /// objects by invoking delegates. The default return value for the <see cref="CanExecute"/>
    /// method is <see langword="true"/>. This type does not allow you to accept command parameters
    /// in the <see cref="Execute"/> and <see cref="CanExecute"/> callback methods.
    /// </summary>
    public sealed class RelayCommand : IRelayCommand
    {
        /// <summary>
        /// The <see cref="Action"/> to invoke when <see cref="Execute"/> is used.
        /// </summary>
        private readonly Action execute;

        /// <summary>
        /// The optional action to invoke when <see cref="CanExecute"/> is used.
        /// </summary>
        private readonly Func<bool>? canExecute;

        /// <inheritdoc/>
        public event EventHandler? CanExecuteChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="RelayCommand"/> class that can always execute.
        /// </summary>
        /// <param name="execute">The execution logic.</param>
        public RelayCommand(Action execute)
        {
            this.execute = execute;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RelayCommand"/> class.
        /// </summary>
        /// <param name="execute">The execution logic.</param>
        /// <param name="canExecute">The execution status logic.</param>
        public RelayCommand(Action execute, Func<bool> canExecute)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }

        /// <inheritdoc/>
        public void NotifyCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanExecute(object? parameter)
        {
            return this.canExecute?.Invoke() != false;
        }

        /// <inheritdoc/>
        public void Execute(object? parameter)
        {
            if (CanExecute(parameter))
            {
                this.execute();
            }
        }
    }
}
