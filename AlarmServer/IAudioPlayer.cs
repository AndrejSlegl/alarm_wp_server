using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlarmServer
{
    public interface IAudioPlayer : IDisposable
    {
        Uri SourceUri { get; }

        void Play(Uri sourceUri, bool isLooping);

        void Stop();
    }
}
