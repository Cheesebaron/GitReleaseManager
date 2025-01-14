﻿// -----------------------------------------------------------------------
// <copyright file="CommentsObjectGraphVisitor.cs" company="GitTools Contributors">
// Copyright (c) 2015 - Present - GitTools Contributors
// </copyright>
// -----------------------------------------------------------------------
// All of the classes in this file have been aquired from
// https://dotnetfiddle.net/8M6iIE which was mentioned
// on the YamlDotNet repository here: https://github.com/aaubry/YamlDotNet/issues/444#issuecomment-546709672

namespace GitReleaseManager.Core.Configuration.CommentSerialization
{
    using YamlDotNet.Core;
    using YamlDotNet.Core.Events;
    using YamlDotNet.Serialization;
    using YamlDotNet.Serialization.ObjectGraphVisitors;

    public sealed class CommentsObjectGraphVisitor : ChainedObjectGraphVisitor
    {
        public CommentsObjectGraphVisitor(IObjectGraphVisitor<IEmitter> nextVisitor)
            : base(nextVisitor)
        {
        }

        public override bool EnterMapping(IPropertyDescriptor key, IObjectDescriptor value, IEmitter context)
        {
            if (value is CommentsObjectDescriptor commentsDescriptor && commentsDescriptor.Comment != null)
            {
                context?.Emit(new Comment(commentsDescriptor.Comment, false));
            }

            return base.EnterMapping(key, value, context);
        }
    }
}