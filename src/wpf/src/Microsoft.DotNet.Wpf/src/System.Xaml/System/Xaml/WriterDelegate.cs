﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace System.Xaml
{
    // This is the simplest implementation of a Node based XamlWriter.
    // It turns XamlWriter calls into nodes and passes them up to the
    // provided _addDelegate.
    //
    internal class WriterDelegate : XamlWriter, IXamlLineInfoConsumer
    {
        private XamlNodeAddDelegate _addDelegate;
        private XamlLineInfoAddDelegate _addLineInfoDelegate;
        private XamlSchemaContext _schemaContext;

        public WriterDelegate(XamlNodeAddDelegate add, XamlLineInfoAddDelegate addlineInfoDelegate, XamlSchemaContext xamlSchemaContext)
        {
            _addDelegate = add;
            _addLineInfoDelegate = addlineInfoDelegate;
            _schemaContext = xamlSchemaContext;
        }

        #region XamlWriter Members

        public override void WriteGetObject()
        {
            ThrowIsDisposed();
            _addDelegate(XamlNodeType.GetObject, null);
        }

        public override void WriteStartObject(XamlType xamlType)
        {
            ThrowIsDisposed();
            _addDelegate(XamlNodeType.StartObject, xamlType);
        }

        public override void WriteEndObject()
        {
            ThrowIsDisposed();
            _addDelegate(XamlNodeType.EndObject, null);
        }

        public override void WriteStartMember(XamlMember member)
        {
            ThrowIsDisposed();
            _addDelegate(XamlNodeType.StartMember, member);
        }

        public override void WriteEndMember()
        {
            ThrowIsDisposed();
            _addDelegate(XamlNodeType.EndMember, null);
        }

        public override void WriteValue(object value)
        {
            ThrowIsDisposed();
            _addDelegate(XamlNodeType.Value, value);
        }

        public override void WriteNamespace(NamespaceDeclaration namespaceDeclaration)
        {
            ThrowIsDisposed();
            _addDelegate(XamlNodeType.NamespaceDeclaration, namespaceDeclaration);
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing && !IsDisposed)
                {
                    _addDelegate(XamlNodeType.None, XamlNode.InternalNodeType.EndOfStream);
                    _addDelegate = delegate { throw new XamlException(SR.WriterIsClosed); };
                    if (_addLineInfoDelegate is not null)
                    {
                        _addLineInfoDelegate = delegate { throw new XamlException(SR.WriterIsClosed); };
                    }
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        public override XamlSchemaContext SchemaContext
        {
            get { return _schemaContext; }
        }
        #endregion

        #region IConsumeXamlLineInfo Members
        /// <summary>
        /// </summary>
        /// <param name="lineNumber"></param>
        /// <param name="linePosition"></param>
        public void SetLineInfo(int lineNumber, int linePosition)
        {
            ThrowIsDisposed();
            _addLineInfoDelegate(lineNumber, linePosition);
        }

        public bool ShouldProvideLineInfo
        {
            get
            {
                ThrowIsDisposed();
                return _addLineInfoDelegate is not null;
            }
        }
        #endregion

        private void ThrowIsDisposed()
        {
            ObjectDisposedException.ThrowIf(IsDisposed, typeof(XamlWriter)); // Can't say ReaderMultiIndexDelegate because its internal.
        }
    }
}
