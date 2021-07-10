namespace SigmaDraconis.Shared
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Xna.Framework;

    public class ShadowModel
    {
        private List<Vector3> shadowModel = new List<Vector3>();
        public bool IsShadowInvalidated { get; set; }
        public bool HasShadowModel => this.shadowModel.Any();

        public List<Vector3> GetModel()
        {
            return this.shadowModel;
        }

        public void SetModel(List<Vector3> model, object owner)
        {
            this.shadowModel = model.ToList();
            EventManager.RaiseEvent(EventType.Shadow, EventSubType.Updated, owner);
        }
    }
}
