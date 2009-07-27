﻿/*
 * Copyright (c) 2009, Peter Nelson (charn.opcode@gmail.com)
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 * 
 * * Redistributions of source code must retain the above copyright notice, 
 *   this list of conditions and the following disclaimer.
 * * Redistributions in binary form must reproduce the above copyright notice, 
 *   this list of conditions and the following disclaimer in the documentation 
 *   and/or other materials provided with the distribution.
 *   
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" 
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE 
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE 
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE 
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR 
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF 
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN 
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) 
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE 
 * POSSIBILITY OF SUCH DAMAGE.
*/

/// TODO:
/// garbage collection / deinitialization
/// either find out if we can remove the need for this stuff altogether - 
/// embedding the manifest into a client application seems to work but is
/// not an ideal solution - or work out how to load it from a resource - 
/// I've tried this already by getting CreateActCtx to load this assembly,
/// but it can't seem to find the manifest resource.

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using WebKit;

namespace WebKit
{
    /// <summary>
    /// A simple interface to the Windows Activation Context API.
    /// </summary>
    /// <remarks>
    /// Activation context switching is required here to support registration
    /// free COM interop.  Ordinarily this can be achieved by embedding an
    /// application manifest with mappings to COM objects in the assembly,
    /// however this does not work in a class library. (TODO: verify this...)
    /// Instead we create an activation context which explicitly loads a
    /// manifest and activate this context when we need to create a COM object.
    /// </remarks>
    internal class ActivationContext
    {
        // Read-only state properties
        public string ManifestFileName { get; private set; }
        public bool Activated { get; private set; }
        public bool Initialized { get; private set; }

        // Private stuff...
        private W32API.ACTCTX activationContext;
        private IntPtr contextHandle;
        private uint lastCookie;

        /// <summary>
        /// Constructor for ActivationContext.
        /// </summary>
        /// <param name="ManifestFileName">Path of the manifest file to load.</param>
        public ActivationContext(string ManifestFileName)
        {
            this.ManifestFileName = ManifestFileName;
            this.Activated = false;
            this.Initialized = false;
        }

        /// <summary>
        /// Activates the activation context.
        /// </summary>
        /// <returns>Success value.</returns>
        public bool Activate()
        {
            if (!Initialized)
                throw new InvalidOperationException("ActivationContext has not been initialized");
            if (!Activated)
            {
                lastCookie = 0;
                Activated = W32API.ActivateActCtx(contextHandle, out lastCookie);
            }
            return Activated;
        }

        /// <summary>
        /// Deactivates the activation context, activating the next one down
        /// on the 'stack'.
        /// </summary>
        /// <returns>Success value.</returns>
        public bool Deactivate()
        {
            if (!Initialized)
                throw new InvalidOperationException("ActivationContext has not been initialized");
            if (Activated)
            {
                // TODO: Error handling?
                W32API.DeactivateActCtx(0, lastCookie);
                Activated = false;
            }
            return true;
        }

        /// <summary>
        /// Initializes the activation context.
        /// </summary>
        public void Initialize()
        {
            if (!Initialized)
            {
                activationContext = new W32API.ACTCTX();

                activationContext.cbSize = Marshal.SizeOf(typeof(W32API.ACTCTX));
                activationContext.lpSource = this.ManifestFileName;

                contextHandle = W32API.CreateActCtx(ref activationContext);

                Initialized = (contextHandle.ToInt32() != -1);
            }
        }
    }
}