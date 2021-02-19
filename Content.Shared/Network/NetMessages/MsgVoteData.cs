﻿using System;
using Lidgren.Network;
using Robust.Shared.Network;

namespace Content.Shared.Network.NetMessages
{
    public sealed class MsgVoteData : NetMessage
    {
        #region REQUIRED

        public const MsgGroups GROUP = MsgGroups.Command;
        public const string NAME = nameof(MsgVoteData);

        public MsgVoteData(INetChannel channel) : base(NAME, GROUP) { }

        #endregion

        public int VoteId;
        public bool VoteActive;
        public string VoteTitle;
        public string VoteInitiator;
        public TimeSpan StartTime; // Server RealTime.
        public TimeSpan EndTime; // Server RealTime.
        public (ushort votes, string name)[] Options;
        public bool IsYourVoteDirty;
        public byte? YourVote;

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            VoteId = buffer.ReadVariableInt32();
            VoteActive = buffer.ReadBoolean();
            buffer.ReadPadBits();

            if (!VoteActive)
                return;

            VoteTitle = buffer.ReadString();
            VoteInitiator = buffer.ReadString();
            StartTime = TimeSpan.FromTicks(buffer.ReadInt64());
            EndTime = TimeSpan.FromTicks(buffer.ReadInt64());

            Options = new (ushort votes, string name)[buffer.ReadByte()];
            for (var i = 0; i < Options.Length; i++)
            {
                Options[i] = (buffer.ReadUInt16(), buffer.ReadString());
            }

            IsYourVoteDirty = buffer.ReadBoolean();
            if (IsYourVoteDirty)
            {
                YourVote = buffer.ReadBoolean() ? buffer.ReadByte() : null;
            }
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.WriteVariableInt32(VoteId);
            buffer.Write(VoteActive);
            buffer.WritePadBits();

            if (!VoteActive)
                return;

            buffer.Write(VoteTitle);
            buffer.Write(VoteInitiator);
            buffer.Write(StartTime.Ticks);
            buffer.Write(EndTime.Ticks);

            buffer.Write((byte) Options.Length);
            foreach (var (votes, name) in Options)
            {
                buffer.Write(votes);
                buffer.Write(name);
            }

            buffer.Write(IsYourVoteDirty);
            if (IsYourVoteDirty)
            {
                buffer.Write(YourVote.HasValue);
                if (YourVote.HasValue)
                {
                    buffer.Write(YourVote.Value);
                }
            }
        }

        public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.ReliableOrdered;
    }
}