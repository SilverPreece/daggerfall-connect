﻿// Project:         Deep Engine
// Description:     3D game engine for Ruins of Hill Deep and Daggerfall Workshop projects.
// Copyright:       Copyright (C) 2012 Gavin Clayton
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Web Site:        http://www.dfworkshop.net
// Contact:         Gavin Clayton (interkarma@dfworkshop.net)
// Project Page:    http://code.google.com/p/daggerfallconnect/

#region Using Statements
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using SceneEditor.Documents;
#endregion

namespace SceneEditor.Proxies
{

    /// <summary>
    /// Base proxy interface.
    /// </summary>
    internal interface IBaseEditorProxy : IEditorProxy { }

    /// <summary>
    /// Defines base requirements of editor proxies.
    /// </summary>
    internal abstract class BaseEditorProxy : IBaseEditorProxy, IEditorProxy
    {

        #region Fields

        string name;
        SceneDocument document;

        #endregion

        #region Properties

        /// <summary>
        /// Gets scene document this proxy belongs to.
        /// </summary>
        [Browsable(false)]
        public SceneDocument SceneDocument
        {
            get { return document; }
        }

        /// <summary>
        /// Gets or sets proxy name.
        /// </summary>
        [Browsable(true)]
        public string Name
        {
            get { return name; }
            set
            {
                SceneDocument.PushUndo(this, this.GetType().GetProperty("Name"));
                name = value;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="document">Scene document.</param>
        public BaseEditorProxy(SceneDocument document)
        {
            // Save references
            this.document = document;
        }

        #endregion

    }

}
