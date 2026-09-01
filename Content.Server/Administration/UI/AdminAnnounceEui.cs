using Content.Server.Administration.Managers;
using Content.Server.Chat;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.EUI;
using Content.Server._Corvax.TTS;
using Content.Shared.Administration;
using Content.Shared._Corvax.TTS;
using Content.Shared.Eui;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.UI
{
    public sealed partial class AdminAnnounceEui : BaseEui
    {
        [Dependency] private IAdminManager _adminManager = default!;
        [Dependency] private IChatManager _chatManager = default!;
        private readonly ChatSystem _chatSystem;
        private readonly TTSSystem _ttsSystem; // Respiral

        public AdminAnnounceEui()
        {
            IoCManager.InjectDependencies(this);
            var sysMan = IoCManager.Resolve<IEntitySystemManager>(); // Respiral
            _chatSystem = sysMan.GetEntitySystem<ChatSystem>(); // Respiral
            _ttsSystem = sysMan.GetEntitySystem<TTSSystem>(); // Respiral
        }

        public override void Opened()
        {
            StateDirty();
        }

        public override EuiStateBase GetNewState()
        {
            return new AdminAnnounceEuiState();
        }

        public override void HandleMessage(EuiMessageBase msg)
        {
            base.HandleMessage(msg);

            switch (msg)
            {
                case AdminAnnounceEuiMsg.DoAnnounce doAnnounce:
                    if (!_adminManager.HasAdminFlag(Player, AdminFlags.Admin))
                    {
                        Close();
                        break;
                    }

                    switch (doAnnounce.AnnounceType)
                    {
                        case AdminAnnounceType.Server:
                            _chatManager.DispatchServerAnnouncement(doAnnounce.Announcement);
                            break;
                        // TODO: Per-station announcement support
                        case AdminAnnounceType.Station:
                            _chatSystem.DispatchGlobalAnnouncement(doAnnounce.Announcement, doAnnounce.Announcer, colorOverride: Color.Gold);
                            break;
                    }

                    // Respiral: озвучка объявления выбранным голосом всем игрокам
                    if (!string.IsNullOrEmpty(doAnnounce.Voice))
                        _ttsSystem.PlayTTS(doAnnounce.Announcement, new ProtoId<TTSVoicePrototype>(doAnnounce.Voice), Filter.Broadcast());

                    StateDirty();

                    if (doAnnounce.CloseAfter)
                        Close();

                    break;
            }
        }
    }
}
