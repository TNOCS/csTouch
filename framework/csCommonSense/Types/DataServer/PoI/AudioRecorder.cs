using System;
using System.IO;
using Caliburn.Micro;

namespace DataServer
{
    // TODO Implement AudioRecorder class, or remove it.
    public class AudioRecorder : PropertyChangedBase
    {
        //private Microphone _microphone;
        //private MemoryStream _stream;
        private byte[] _buffer; // FIXME TODO _buffer is assigned but not used.
        //private PoiService service;
        //private Event pptEvent;
        //private Media media;

        private bool active;

        public bool Active
        {
            get { return active; }
            set { active = value; NotifyOfPropertyChange(() => Active); }
        }


        public bool StartRecording(Media ppt, Event e, PoiService ps)
        {
            //media = ppt;
            //service = ps;
            //pptEvent = e;
            if (Active) return false;
            //_stream = new MemoryStream();
            //_stream.Seek(0, SeekOrigin.Begin);
            //_microphone = Microphone.Default;
            //_microphone.BufferReady -= microphone_BufferReady;
            //_microphone.BufferReady += microphone_BufferReady;

            //_microphone.BufferDuration = TimeSpan.FromMilliseconds(300);
            //_buffer = new byte[_microphone.GetSampleSizeInBytes(_microphone.BufferDuration)];


            //_microphone.Start();
            return true;
        }

        private void microphone_BufferReady(object sender, EventArgs e)
        {
            //_microphone.GetData(_buffer);
            //_stream.Write(_buffer, 0, _buffer.Length);
            //service.AudioStream.SignalBuffer(service.client.Id, _buffer);

            ////TalkItem ti = TalkItems.FirstOrDefault(k => k.Sender == Imb.Id && k.State == "recording");
            //if (media != null)
            //{
            //    media.Packages.AddRange(_buffer);
            //    media.UpdateDuration();
            //}
        }

        internal void StopRecording()
        {
            //if (_microphone.State == MicrophoneState.Started)
            //{
            //    _microphone.GetData(_buffer);
            //    _stream.Write(_buffer, 0, _buffer.Length);
            //    service.AudioStream.SignalBuffer(service.client.Id, _buffer);
            //    if (media != null) media.Packages.AddRange(_buffer);
            //    //ti.Completed = true;
            //    //ti.State = "recorded";
            //    media.UpdateDuration();
            //    _microphone.Stop();
            //    service.AudioStream.SignalBuffer(service.client.Id, new byte[0]);
            //}
        }
    }
}
