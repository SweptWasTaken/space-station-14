using System;
using Content.Server.Administration;
using Content.Server.Commands;
using Content.Shared.Actions;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.IoC;

namespace Content.Server.Actions.Commands
{
    [AdminCommand(AdminFlags.Debug)]
    public sealed class GrantAction : IConsoleCommand
    {
        public string Command => "grantaction";
        public string Description => "Grants an action to a player, defaulting to current player";
        public string Help => "grantaction <actionType> <name or userID, omit for current player>";
        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            if (player == null) return;
            var attachedEntity = player.AttachedEntity;
            if (args.Length > 1)
            {
                var target = args[1];
                if (!CommandUtils.TryGetAttachedEntityByUsernameOrId(shell, target, player, out attachedEntity)) return;
            }

            if (attachedEntity == null) return;
            if (!attachedEntity.TryGetComponent(out ServerActionsComponent? actionsComponent))
            {
                shell.WriteLine("user has no actions component");
                return;
            }

            var actionTypeRaw = args[0];
            if (!Enum.TryParse<ActionType>(actionTypeRaw, out var actionType))
            {
                shell.WriteLine("unrecognized ActionType enum value, please" +
                                " ensure you used correct casing: " + actionTypeRaw);
                return;
            }
            var actionMgr = IoCManager.Resolve<ActionManager>();
            if (!actionMgr.TryGet(actionType, out var action))
            {
                shell.WriteLine("unrecognized actionType " + actionType);
                return;
            }
            actionsComponent.Grant(action.ActionType);
        }
    }
}
