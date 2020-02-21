using System;
using Nekoyume.Action;

namespace Nekoyume.Model.Mail
{
    [Serializable]
    public class ItemEnhanceMail : AttachmentMail
    {
        protected override string TypeId => "itemEnhance";
        public override MailType MailType => MailType.Workshop;

        public ItemEnhanceMail(AttachmentActionResult attachmentActionResult, long blockIndex, Guid id) : base(attachmentActionResult, blockIndex, id)
        {

        }

        public ItemEnhanceMail(Bencodex.Types.Dictionary serialized)
            : base(serialized)
        {
        }

        public override void Read(IMail mail)
        {
            mail.Read(this);
        }
    }
}
