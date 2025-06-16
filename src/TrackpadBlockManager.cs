using System;

namespace tfd
{
    public class TrackpadBlockManager
    {
        public TrackpadBlockManager(IContext appContext)
        {
            //this.Logger = appContext.GetLogger();
        }

        public void ProcessTouch(TrackpadContact[] contacts)
        {
            if (contacts == null) return;
        }
    }
}
