﻿using System;
using DeltaBotFour.Infrastructure.Interface;
using DeltaBotFour.Models;
using DeltaBotFour.Reddit.Interface;

namespace DeltaBotFour.Infrastructure.Implementation.PrivateMessageHandlers
{
    public class ModAddDeltaPMHandler : IPrivateMessageHandler
    {
        private const string AddSucceededMessage = "Delta has been added.";
        private const string AddFailedAlreadyAwardedMessage = "I already successfully awarded a delta for this comment. I can't do 2 for the same comment.";
        private const string AddFailedErrorMessageFormat = "Add failed. DeltaBot is very sorry :(\n\nSend this to a DeltaBot dev:\n\n{0}";

        private readonly IRedditService _redditService;
        private readonly ICommentReplyDetector _replyDetector;
        private readonly ICommentReplyBuilder _replyBuilder;
        private readonly ICommentReplier _replier;
        private readonly IDeltaAwarder _deltaAwarder;

        public ModAddDeltaPMHandler(IRedditService redditService,
            ICommentReplyDetector replyDetector,
            ICommentReplyBuilder replyBuilder,
            ICommentReplier replier,
            IDeltaAwarder deltaAwarder)
        {
            _redditService = redditService;
            _replyDetector = replyDetector;
            _replyBuilder = replyBuilder;
            _replier = replier;
            _deltaAwarder = deltaAwarder;
        }

        public void Handle(DB4Thing privateMessage)
        {
            // The body should be the URL to a comment
            string commentUrl = privateMessage.Body.Trim();

            try
            {
                // Get comment by url
                var comment = _redditService.GetCommentByUrl(commentUrl);

                // If that succeeded, we need the full comment with children to check for replies
                _redditService.PopulateParentAndChildren(comment);

                // Check for replies
                var db4ReplyResult = _replyDetector.DidDB4Reply(comment);

                // If a delta was already awarded successfully, bail
                if (db4ReplyResult.HasDB4Replied && db4ReplyResult.WasSuccessReply)
                {
                    _redditService.ReplyToPrivateMessage(privateMessage.Id,
                        AddFailedAlreadyAwardedMessage);

                    return;
                }

                // Force award a delta - it doesn't matter if there's a reply, mods can award deltas to any comment
                _deltaAwarder.Award(comment);

                // Build moderator add message
                var reply = _replyBuilder.Build(DeltaCommentReplyType.ModeratorAdded, comment);

                // Don't edit the existing comment - delete it and reply with the mod added reply
                _replier.DeleteReply(db4ReplyResult.Comment);
                _replier.Reply(comment, reply);

                // Reply to moderator indicating success
                _redditService.ReplyToPrivateMessage(privateMessage.Id,
                    AddSucceededMessage);
            }
            catch (Exception ex)
            {
                // Reply indicating failure
                _redditService.ReplyToPrivateMessage(privateMessage.Id,
                    string.Format(AddFailedErrorMessageFormat, ex.ToString()));

                // Rethrow for logging purposes
                throw;
            }
        }
    }
}
