#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using IMB3.ByteBuffers;

#endregion

namespace IMB3
{
    public class TEventNameEntry
    {
        public string EventName;
        public int Publishers;
        public int Subscribers;
        public int Timers;
    }

    public class TEventEntry
    {
        public delegate void TOnBuffer(TEventEntry aEvent, Int32 aTick, Int32 aBufferID, TByteBuffer aBuffer);

        public delegate void TOnChangeFederation(TConnection aConnection, Int32 aNewFederationID, string aNewFederation);

        public delegate void TOnChangeObject(Int32 aAction, Int32 aObjectID, string aObjectName, string aAttribute);

        public delegate void TOnChangeObjectData(
            TEventEntry aEvent, Int32 aAction, Int32 aObjectID, string aAttribute, TByteBuffer aNewValues,
            TByteBuffer aOldValues);

        public delegate void TOnFocus(double x, double y);

        public delegate void TOnNormalEvent(TEventEntry aEvent, TByteBuffer aPayload);

        public delegate void TOnOtherEvent(TEventEntry aEvent, Int32 aTick, TEventKind aEventKind, TByteBuffer aPayload);

        public delegate Stream TOnStreamCreate(TEventEntry aEvent, string aStreamName);

        public delegate void TOnStreamEnd(TEventEntry aEvent, ref Stream aStream, string aStreamName);

        public delegate void TOnSubAndPubEvent(TEventEntry aEvent, TConnectionPlatform.TCommands aCommand);

        public delegate void TOnTimerCmd(TEventEntry aEvent, TEventKind aEventKind, string aTimerName);

        public delegate void TOnTimerTick(
            TEventEntry aEvent, string aTimerName, Int32 aTick, Int64 aTickTime, Int64 aStartTime);

        public enum TEventKind
        {
            ekChangeObjectEvent           = 0, // imb version 1
            ekStreamHeader                = 1,
            ekStreamBody                  = 2,
            ekStreamTail                  = 3,
            ekBuffer                      = 4,
            ekNormalEvent                 = 5,
            ekChangeObjectDataEvent       = 6,
            ekLogWriteLn                  = 30,
            ekTimerCancel                 = 40,
            ekTimerPrepare                = 41,
            ekTimerStart                  = 42,
            ekTimerStop                   = 43,
            ekTimerAcknowledgedListAdd    = 45,
            ekTimerAcknowledgedListRemove = 46,
            ekTimerSetSpeed               = 47,
            ekTimerTick                   = 48,
            ekTimerAcknowledge            = 49,
            ekTimerStatusRequest          = 50
        }

        public enum TLogLevel
        {
            llRemark,
            llDump,
            llNormal,
            llStart,
            llFinish,
            llPush,
            llPop,
            llStamp,
            llSummary,
            llWarning,
            llError
        }

        public   const int trcInfinite      = int.MaxValue;
        // private/internal
        private  const Int32 EventKindMask  = 0x000000FF;
        private  const Int32 EventFlagsMask = 0x0000FF00;
        public   const Int32 actionNew      = 0;
        public   const Int32 actionDelete   = 1;
        public   const Int32 actionChange   = 2;
        private  readonly TStreamCache FStreamCache = new TStreamCache();
        public   readonly Int32 ID;
        public   readonly TConnection connection;
        internal string FEventName;

        private bool FIsPublished;
        private bool FIsSubscribed;
        internal TEventEntry FParent;
        private bool FPublishers;
        private bool FSubscribers;

        public TEventEntry(TConnection aConnection, Int32 aID, string aEventName) {
            connection    = aConnection;
            ID            = aID;
            FEventName    = aEventName;
            FParent       = null;
            FIsPublished  = false;
            FIsSubscribed = false;
            FSubscribers  = false;
            FPublishers   = false;
        }

        internal bool IsEmpty {
            get { return !(FIsSubscribed || FIsPublished); }
        }

        public bool Subscribers {
            get { return FSubscribers; }
        }

        public bool Publishers {
            get { return FPublishers; }
        }

        public string EventName {
            get { return FEventName; }
        }

        public string EventNameWithoutFederation {
            get {
                var federationPrefix = connection.Federation + ".";
                return FEventName.StartsWith(federationPrefix)
                    ? FEventName.Substring(federationPrefix.Length)
                    : FEventName;
            }
        }

        public bool IsPublished {
            get { return FIsPublished; }
        }

        public bool IsSubscribed {
            get { return FIsSubscribed; }
        }

        ~TEventEntry() {
            FStreamCache.Clear();
        }

        private int TimerBasicCmd(TEventKind aEventKind, string aTimerName) {
            var payload = new TByteBuffer();
            payload.Prepare(aTimerName);
            payload.PrepareApply();
            payload.QWrite(aTimerName);
            return SignalEvent(aEventKind, payload.Buffer);
        }

        private int TimerAcknowledgeCmd(TEventKind aEventKind, string aTimerName, string aClientName) {
            var payload = new TByteBuffer();
            payload.Prepare(aTimerName);
            payload.Prepare(aClientName);
            payload.PrepareApply();
            payload.QWrite(aTimerName);
            payload.QWrite(aClientName);
            return SignalEvent(aEventKind, payload.Buffer);
        }

        internal void Subscribe() {
            FIsSubscribed = true;
            // send command
            var payload = new TByteBuffer();
            payload.Prepare(ID);
            payload.Prepare(0); // EET
            payload.Prepare(EventName);
            payload.PrepareApply();
            payload.QWrite(ID);
            payload.QWrite(0); // EET
            payload.QWrite(EventName);
            connection.WriteCommand(TConnectionPlatform.TCommands.icSubscribe, payload.Buffer);
        }

        internal void Publish() {
            FIsPublished = true;
            // send command
            var payload = new TByteBuffer();
            payload.Prepare(ID);
            payload.Prepare(0); // EET
            payload.Prepare(EventName);
            payload.PrepareApply();
            payload.QWrite(ID);
            payload.QWrite(0); // EET
            payload.QWrite(EventName);
            connection.WriteCommand(TConnectionPlatform.TCommands.icPublish, payload.Buffer);
        }

        internal void UnSubscribe(bool aChangeLocalState = true) {
            if (aChangeLocalState)
                FIsSubscribed = false;
            // send command
            var payload = new TByteBuffer();
            payload.Prepare(EventName);
            payload.PrepareApply();
            payload.QWrite(EventName);
            connection.WriteCommand(TConnectionPlatform.TCommands.icUnsubscribe, payload.Buffer);
        }

        internal void UnPublish(bool aChangeLocalState = true) {
            if (aChangeLocalState)
                FIsPublished = false;
            // send command
            var payload = new TByteBuffer();
            payload.Prepare(EventName);
            payload.PrepareApply();
            payload.QWrite(EventName);
            connection.WriteCommand(TConnectionPlatform.TCommands.icUnpublish, payload.Buffer);
        }

        internal void CopyHandlersFrom(TEventEntry aEventEntry) {
            OnChangeObject     = aEventEntry.OnChangeObject;
            OnFocus            = aEventEntry.OnFocus;
            OnNormalEvent      = aEventEntry.OnNormalEvent;
            OnBuffer           = aEventEntry.OnBuffer;
            OnStreamCreate     = aEventEntry.OnStreamCreate;
            OnStreamEnd        = aEventEntry.OnStreamEnd;
            OnChangeFederation = aEventEntry.OnChangeFederation;
            OnTimerTick        = aEventEntry.OnTimerTick;
            OnTimerCmd         = aEventEntry.OnTimerCmd;
            OnChangeObjectData = aEventEntry.OnChangeObjectData;
            OnOtherEvent       = aEventEntry.OnOtherEvent;
            OnSubAndPub        = aEventEntry.OnSubAndPub;
        }

        // dispatcher for all events
        internal void HandleEvent(TByteBuffer aPayload) {
            Int32 EventTick;
            Int32 EventKindInt;
            aPayload.Read(out EventTick);
            aPayload.Read(out EventKindInt);
            var eventKind = (TEventKind) (EventKindInt & EventKindMask);
            switch (eventKind) {
                case TEventKind.ekChangeObjectEvent:
                    HandleChangeObject(aPayload);
                    break;
                case TEventKind.ekChangeObjectDataEvent:
                    HandleChangeObjectData(aPayload);
                    break;
                case TEventKind.ekBuffer:
                    HandleBuffer(EventTick, aPayload);
                    break;
                case TEventKind.ekNormalEvent:
                    if (OnNormalEvent != null)
                        OnNormalEvent(this, aPayload);
                    break;
                case TEventKind.ekTimerTick:
                    HandleTimerTick(aPayload);
                    break;
                case TEventKind.ekTimerPrepare:
                case TEventKind.ekTimerStart:
                case TEventKind.ekTimerStop:
                    HandleTimerCmd(eventKind, aPayload);
                    break;
                case TEventKind.ekStreamHeader:
                case TEventKind.ekStreamBody:
                case TEventKind.ekStreamTail:
                    HandleStreamEvent(eventKind, aPayload);
                    break;
                default:
                    if (OnOtherEvent != null)
                        OnOtherEvent(this, EventTick, eventKind, aPayload);
                    break;
            }
        }

        // dispatchers for specific events
        private void HandleChangeObject(TByteBuffer aPayload) {
            if (OnFocus != null) {
                double x;
                double y;
                aPayload.Read(out x);
                aPayload.Read(out y);
                OnFocus(x, y);
            }
            else {
                if (OnChangeFederation != null) {
                    Int32 Action;
                    Int32 NewFederationID;
                    string NewFederation;
                    aPayload.Read(out Action);
                    aPayload.Read(out NewFederationID);
                    aPayload.Read(out NewFederation);
                    OnChangeFederation(connection, NewFederationID, NewFederation);
                }
                else {
                    if (OnChangeObject == null) return;
                    Int32 Action;
                    Int32 ObjectID;
                    string Attribute;

                    aPayload.Read(out Action);
                    aPayload.Read(out ObjectID);
                    aPayload.Read(out Attribute);
                    OnChangeObject(Action, ObjectID, EventName, Attribute);
                }
            }
        }

        private void HandleChangeObjectData(TByteBuffer aPayload) {
            if (OnChangeObjectData == null) return;
            Int32 action;
            Int32 objectID;
            string attribute;
            aPayload.Read(out action);
            aPayload.Read(out objectID);
            aPayload.Read(out attribute);
            var newValues = aPayload.ReadByteBuffer();
            var oldValues = aPayload.ReadByteBuffer();
            OnChangeObjectData(this, action, objectID, attribute, newValues, oldValues);
        }

        private void HandleBuffer(Int32 aEventTick, TByteBuffer aPayload) {
            if (OnBuffer == null) return;
            var bufferID = aPayload.ReadInt32();
            var buffer = aPayload.ReadByteBuffer();
            OnBuffer(this, aEventTick, bufferID, buffer);
        }

        private void HandleTimerTick(TByteBuffer aPayload) {
            if (OnTimerTick == null) return;
            string timerName;
            Int32 tick;
            Int64 tickTime;
            Int64 startTime;
            aPayload.Read(out timerName);
            aPayload.Read(out tick);
            aPayload.Read(out tickTime);
            aPayload.Read(out startTime);
            OnTimerTick(this, timerName, tick, tickTime, startTime);
        }

        private void HandleTimerCmd(TEventKind aEventKind, TByteBuffer aPayload) {
            if (OnTimerCmd == null) return;
            string TimerName;
            aPayload.Read(out TimerName);
            OnTimerCmd(this, aEventKind, TimerName);
        }

        private void HandleStreamEvent(TEventKind aEventKind, TByteBuffer aPayload) {
            Int32 streamID;
            string streamName;
            Stream stream;
            switch (aEventKind) {
                case TEventKind.ekStreamHeader:
                    if (OnStreamCreate != null) {
                        aPayload.Read(out streamID);
                        aPayload.Read(out streamName);
                        stream = OnStreamCreate(this, streamName);
                        if (stream != null)
                            FStreamCache.Cache(streamID, stream, streamName);
                    }
                    break;
                case TEventKind.ekStreamBody:
                    aPayload.Read(out streamID);
                    stream = FStreamCache.Find(streamID, out streamName);
                    if (stream != null)
                        stream.Write(aPayload.Buffer, aPayload.ReadCursor, aPayload.ReadAvailable);
                    break;
                case TEventKind.ekStreamTail:
                    aPayload.Read(out streamID);
                    stream = FStreamCache.Find(streamID, out streamName);
                    if (stream != null) {
                        stream.Write(aPayload.Buffer, aPayload.ReadCursor, aPayload.ReadAvailable);
                        if (OnStreamEnd != null)
                            OnStreamEnd(this, ref stream, streamName);
                        stream.Close();
                        FStreamCache.Remove(streamID);
                    }
                    break;
            }
        }

        internal void HandleOnSubAndPub(TConnectionPlatform.TCommands aCommand) {
            if (FParent == null) {
                if (OnSubAndPub != null)
                    OnSubAndPub(this, aCommand);
            }
            switch (aCommand) {
                case TConnectionPlatform.TCommands.icSubscribe:
                    if (FParent != null && !IsPublished)
                        Publish();
                    FSubscribers = true;
                    break;
                case TConnectionPlatform.TCommands.icPublish:
                    if (FParent != null && !IsSubscribed)
                        Subscribe();
                    FPublishers = true;
                    break;
                case TConnectionPlatform.TCommands.icUnsubscribe:
                    if (FParent != null && IsPublished)
                        UnPublish();
                    FSubscribers = false;
                    break;
                case TConnectionPlatform.TCommands.icUnpublish:
                    if (FParent != null && IsSubscribed)
                        UnSubscribe();
                    FPublishers = false;
                    break;
            }
        }

        // public

        public event TOnChangeObject OnChangeObject = null;
        public event TOnFocus OnFocus = null;
        // imb 2

        public event TOnNormalEvent OnNormalEvent = null;
        public event TOnBuffer OnBuffer = null;
        public event TOnStreamCreate OnStreamCreate = null;
        public event TOnStreamEnd OnStreamEnd = null;
        public event TOnChangeFederation OnChangeFederation = null;
        // imb 3

        public event TOnTimerTick OnTimerTick = null;
        public event TOnTimerCmd OnTimerCmd = null;
        public event TOnChangeObjectData OnChangeObjectData = null;
        public event TOnSubAndPubEvent OnSubAndPub = null;
        public event TOnOtherEvent OnOtherEvent = null;
        // signals (send events)
        public int SignalEvent(TEventKind aEventKind, byte[] aEventPayload) {
            var payload = new TByteBuffer();
            if (!IsPublished && connection.AutoPublish)
                Publish();
            if (IsPublished) {
                payload.Prepare(ID);
                payload.Prepare((Int32) 0); // tick
                payload.Prepare((Int32) aEventKind);
                payload.Prepare(aEventPayload);
                payload.PrepareApply();
                payload.QWrite(ID);
                payload.QWrite((Int32) (0)); // tick
                payload.QWrite((Int32) aEventKind);
                payload.QWrite(aEventPayload);
                return connection.WriteCommand(TConnectionPlatform.TCommands.icEvent, payload.Buffer);
            }
            else
                return TConnection.iceNotEventPublished;
        }

        public int SignalBuffer(Int32 aBufferID, byte[] aBuffer, Int32 aEventFlags = 0) {
            var payload = new TByteBuffer();
            if (!IsPublished && connection.AutoPublish)
                Publish();
            if (IsPublished) {
                payload.Prepare(ID);
                payload.Prepare((Int32) 0); // tick
                payload.Prepare((Int32) TEventKind.ekBuffer | (aEventFlags & EventFlagsMask));
                payload.Prepare(aBufferID);
                payload.Prepare(aBuffer.Length);
                payload.Prepare(aBuffer);
                payload.PrepareApply();
                payload.QWrite(ID);
                payload.QWrite((Int32) (0)); // tick
                payload.QWrite((Int32) TEventKind.ekBuffer | (aEventFlags & EventFlagsMask));
                payload.QWrite(aBufferID);
                payload.QWrite(aBuffer.Length);
                payload.QWrite(aBuffer);
                return connection.WriteCommand(TConnectionPlatform.TCommands.icEvent, payload.Buffer);
            }
            else
                return TConnection.iceNotEventPublished;
        }

        private static int ReadBytesFromStream(TByteBuffer aBuffer, Stream aStream) {
            try {
                var count = 0;
                var numBytesRead = -1;
                while (aBuffer.WriteAvailable > 0 && numBytesRead != 0) {
                    numBytesRead = aStream.Read(aBuffer.Buffer, aBuffer.WriteCursor, aBuffer.WriteAvailable);
                    aBuffer.Written(numBytesRead);
                    count += numBytesRead;
                }
                return count;
            }
            catch (IOException) {
                return 0; // signal stream read error
            }
        }

        public int SignalStream(string aStreamName, Stream aStream) {
            var payload = new TByteBuffer();
            if (!IsPublished && connection.AutoPublish)
                Publish();
            if (IsPublished) {
                // ekStreamHeader, includes stream name, no stream data
                var streamNameUtf8 = Encoding.UTF8.GetBytes(aStreamName);
                var streamID = connection.getConnectionHashCode(streamNameUtf8);
                payload.Prepare(ID);
                payload.Prepare((Int32) 0); // tick
                payload.Prepare((Int32) TEventKind.ekStreamHeader); // event kind
                payload.Prepare(streamID);
                payload.Prepare(aStreamName);
                payload.PrepareApply();
                payload.QWrite(ID);
                payload.QWrite((Int32) 0); // tick
                var eventKindIndex = payload.WriteCursor;
                payload.QWrite((Int32) TEventKind.ekStreamHeader); // event kind
                payload.QWrite(streamID);
                var bodyIndex = payload.WriteCursor;
                payload.QWrite(aStreamName);
                var res = connection.WriteCommand(TConnectionPlatform.TCommands.icEvent, payload.Buffer);
                if (res > 0) {
                    // ekStreamBody, only buffer size chunks of data
                    // prepare Payload to same value but aStreamName stripped
                    // fixup event kind
                    payload.WriteStart(eventKindIndex);
                    payload.QWrite((Int32) TEventKind.ekStreamBody);
                    payload.WriteStart(bodyIndex);
                    // prepare room for body data
                    payload.PrepareStart();
                    payload.PrepareSize(TConnection.MaxStreamBodyBuffer);
                    payload.PrepareApply();
                    // write pointer in ByteBuffer is still at beginning of stream read buffer!
                    // but buffer is already created on correct length
                    Int32 readSize;
                    do {
                        readSize = ReadBytesFromStream(payload, aStream);
                        //ReadSize = aStream.Read(Payload.Buffer, BodyIndex, Connection.MaxStreamBodyBuffer);
                        if (readSize == TConnection.MaxStreamBodyBuffer)
                            res = connection.WriteCommand(TConnectionPlatform.TCommands.icEvent, payload.Buffer);
                        // reset write position
                        payload.WriteStart(bodyIndex);
                    } while ((readSize == TConnection.MaxStreamBodyBuffer) && (res > 0));
                    if (res > 0) {
                        // clip ByteBuffer to bytes read from stream
                        // write pointer in ByteBuffer is still at beginning of stream read buffer!
                        payload.PrepareStart();
                        payload.PrepareSize(readSize);
                        payload.PrepareApplyAndTrim();
                        // fixup event kind
                        payload.WriteStart(eventKindIndex);
                        payload.QWrite((Int32) TEventKind.ekStreamTail);
                        res = connection.WriteCommand(TConnectionPlatform.TCommands.icEvent, payload.Buffer);
                    }
                }
                return res;
            }
            else
                return TConnection.iceNotEventPublished;
        }

        public int SignalChangeObject(int aAction, int aObjectID, string aAttribute = "") {
            var Payload = new TByteBuffer();
            if (!IsPublished && connection.AutoPublish)
                Publish();
            if (IsPublished) {
                Payload.Prepare(ID);
                Payload.Prepare((Int32) 0); // tick
                Payload.Prepare((Int32) TEventKind.ekChangeObjectEvent);
                Payload.Prepare(aAction);
                Payload.Prepare(aObjectID);
                Payload.Prepare(aAttribute);
                Payload.PrepareApply();
                Payload.QWrite(ID);
                Payload.QWrite((Int32) (0)); // tick
                Payload.QWrite((Int32) TEventKind.ekChangeObjectEvent);
                Payload.QWrite(aAction);
                Payload.QWrite(aObjectID);
                Payload.QWrite(aAttribute);
                return connection.WriteCommand(TConnectionPlatform.TCommands.icEvent, Payload.Buffer);
            }
            else
                return TConnection.iceNotEventPublished;
        }

        // timers
        public int TimerCreate(string aTimerName, Int64 aStartTimeUTCorRelFT, int aResolutionms, double aSpeedFactor,
            int aRepeatCount = trcInfinite) {
            var Payload = new TByteBuffer();
            if (!IsPublished && connection.AutoPublish)
                Publish();
            if (IsPublished) {
                Payload.Prepare(ID);
                Payload.Prepare(aTimerName);
                Payload.Prepare(aStartTimeUTCorRelFT);
                Payload.Prepare(aResolutionms);
                Payload.Prepare(aSpeedFactor);
                Payload.Prepare(aRepeatCount);
                Payload.PrepareApply();
                Payload.QWrite(ID);
                Payload.QWrite(aTimerName);
                Payload.QWrite(aStartTimeUTCorRelFT);
                Payload.QWrite(aResolutionms);
                Payload.QWrite(aSpeedFactor);
                Payload.QWrite(aRepeatCount);
                return connection.WriteCommand(TConnectionPlatform.TCommands.icCreateTimer, Payload.Buffer);
            }
            else
                return TConnection.iceNotEventPublished;
        }

        public int TimerCancel(string aTimerName) {
            return TimerBasicCmd(TEventKind.ekTimerCancel, aTimerName);
        }

        public int TimerPrepare(string aTimerName) {
            return TimerBasicCmd(TEventKind.ekTimerPrepare, aTimerName);
        }

        public int TimerStart(string aTimerName) {
            return TimerBasicCmd(TEventKind.ekTimerStart, aTimerName);
        }

        public int TimerStop(string aTimerName) {
            return TimerBasicCmd(TEventKind.ekTimerStop, aTimerName);
        }

        public int TimerSetSpeed(string aTimerName, double aSpeedFactor) {
            var Payload = new TByteBuffer();
            Payload.Prepare(aTimerName);
            Payload.Prepare(aSpeedFactor);
            Payload.PrepareApply();
            Payload.QWrite(aTimerName);
            Payload.QWrite(aSpeedFactor);
            return SignalEvent(TEventKind.ekTimerSetSpeed, Payload.Buffer);
        }

        public int TimerAcknowledgeAdd(string aTimerName, string aClientName) {
            return TimerAcknowledgeCmd(TEventKind.ekTimerAcknowledgedListAdd, aTimerName, aClientName);
        }

        public int TimerAcknowledgeRemove(string aTimerName, string aClientName) {
            return TimerAcknowledgeCmd(TEventKind.ekTimerAcknowledgedListRemove, aTimerName, aClientName);
        }

        public int TimerAcknowledge(string aTimerName, string aClientName, int aProposedTimeStep) {
            var Payload = new TByteBuffer();
            Payload.Prepare(aClientName);
            Payload.Prepare(aTimerName);
            Payload.Prepare(aProposedTimeStep);
            Payload.PrepareApply();
            Payload.QWrite(aClientName);
            Payload.QWrite(aTimerName);
            Payload.QWrite(aProposedTimeStep);
            return SignalEvent(TEventKind.ekTimerAcknowledge, Payload.Buffer);
        }

        // log
        public int LogWriteLn(string aLine, TLogLevel aLevel) {
            var Payload = new TByteBuffer();
            if (!IsPublished && connection.AutoPublish)
                Publish();
            if (IsPublished) {
                Payload.Prepare((Int32) 0); // client id filled in by hub
                Payload.Prepare(aLine);
                Payload.Prepare((Int32) aLevel);
                Payload.PrepareApply();
                Payload.QWrite((Int32) 0); // client id filled in by hub
                Payload.QWrite(aLine);
                Payload.QWrite((Int32) aLevel);
                return SignalEvent(TEventKind.ekLogWriteLn, Payload.Buffer);
            }
            else
                return TConnection.iceNotEventPublished;
        }

        // other
        public void ClearAllStreams() {
            FStreamCache.Clear();
        }

        public int SignalString(string p) {
            var SmallTestEventPayload = new TByteBuffer();
            // build payload for small event
            SmallTestEventPayload.Prepare(p);
            SmallTestEventPayload.PrepareApply();
            SmallTestEventPayload.QWrite(p);
            return SignalEvent(TEventKind.ekNormalEvent, SmallTestEventPayload.Buffer);
        }

        private class TStreamCache
        {
            private readonly List<TStreamCacheEntry> FStreamCacheList = new List<TStreamCacheEntry>();

            public int Count {
                get { return FStreamCacheList.Count; }
            }

            public Stream Find(int aStreamID, out string aName) {
                var SCE = new TStreamCacheEntry(aStreamID, null, null);
                var i = FStreamCacheList.IndexOf(SCE);
                if (i >= 0) {
                    aName = FStreamCacheList[i].Name;
                    return FStreamCacheList[i].Stream;
                }
                else {
                    aName = "";
                    return null;
                }
            }

            public void Clear() {
                FStreamCacheList.Clear();
            }

            public void Cache(int aStreamID, Stream aStream, string aStreamName) {
                FStreamCacheList.Add(new TStreamCacheEntry(aStreamID, aStream, aStreamName));
            }

            public void Remove(int aStreamID) {
                FStreamCacheList.Remove(new TStreamCacheEntry(aStreamID, null, null));
            }
        }

        private class TStreamCacheEntry
        {
            private readonly string FName;
            private readonly Stream FStream;
            private readonly int FStreamID;

            public TStreamCacheEntry(int aStreamID, Stream aStream, string aStreamName) {
                FStreamID = aStreamID;
                FStream = aStream;
                FName = aStreamName;
            }

            public int StreamID {
                get { return FStreamID; }
            }

            public Stream Stream {
                get { return FStream; }
            }

            public string Name {
                get { return FName; }
            }

            public override bool Equals(Object obj) {
                var SCE = obj as TStreamCacheEntry;

                if (SCE != null)
                    return FStreamID == SCE.FStreamID;
                else
                    return false;
            }

            public override int GetHashCode() {
                return FStreamID.GetHashCode();
            }
        }
    }

    public class TConnection : TConnectionPlatform
    {
        // constructors/destructor

        public delegate void TOnDisconnect(TConnection aConnection);

        public delegate void TOnEventnames(TConnection aConnection, TEventNameEntry[] aEventNames);

        public delegate void TOnStatusUpdate(
            TConnection aConnection, string aModelUniqueClientID, string aModelName, Int32 aProgress, Int32 aStatus);

        public delegate void TOnSubAndPub(TConnection aConnection, TCommands aCommand, string aEventName);

        public delegate void TOnVariable(TConnection aConnection, string aVarName, byte[] aVarValue, byte[] aPrevValue);

        public enum TConnectionState
        {
            icsUninitialized,
            icsInitialized,
            icsClient,
            icsHub,
            icsEnded,
            // room for extensions ..
            // gateway values are used over network and should be same over all connected clients/brokers
            icsGateway = 100, // equal
            icsGatewayClient = 101, // this gateway acts as a client; subscribes are not received
            icsGatewayServer = 102 // this gateway treats connected broker as client
        }

        public enum TVarPrefix
        {
            vpUniqueClientID,
            vpClientHandle
        }

        private const string ModelStatusVarName = "ModelStatus";
        private const string msVarSepChar = "|";
        private const string EventFilterPostFix = "*";
        // consts
        internal const int MaxStreamBodyBuffer = 16*1024; // in bytes
        private const string FocusEventName = "Focus";
        public const string DefaultFederation = "UST";
        public const int iceConnectionClosed = -1;
        public const int iceNotEventPublished = -2;
        public static readonly Int32 statusReady = 0; // R
        public static readonly Int32 statusCalculating = 1; // C
        public static readonly Int32 statusBusy = 2; // B
        // fields
        private readonly TEventEntryList FEventEntryList = new TEventEntryList();
        private readonly TEventTranslation FEventTranslation = new TEventTranslation();
        private readonly bool FIMB2Compatible;
        private readonly string FederationChangeEventName = "META_CurrentSession";
        private readonly object WriteCommandLock = new object();
        public readonly Int32 efPublishers = 1;
        public readonly Int32 efSubscribers = 2;
        public readonly Int32 efTimers = 4;
        public bool AutoPublish = true;
        // time
        private Int64 FBrokerAbsoluteTime;
        private Int32 FBrokerTick;
        private Int32 FBrokerTickDelta;
        private TEventEntry FChangeFederationEvent;
        private Int32 FClientHandle = 0;
        private string FFederation = DefaultFederation;
        // standard event references
        private TEventEntry FFocusEvent;
        private TEventEntry FLogEvent;
        private Int32 FOwnerID = 0;
        private string FOwnerName = "";
        private string FRemoteHost = "";
        private int FRemotePort = 0;
        private Thread FThread = null;
        private Int32 FUniqueClientID = 0;

        public TConnection(string aHost, int aPort, string aOwnerName, int aOwnerID,
            string aFederation = DefaultFederation, bool aIMB2Compatible = true, bool aStartReadingThread = true) {
            FFederation = aFederation;
            FOwnerName = aOwnerName;
            FOwnerID = aOwnerID;
            FIMB2Compatible = aIMB2Compatible;
            Open(aHost, aPort, aStartReadingThread);
        }

        public string Federation {
            get { return FFederation; }
            set {
                var OldFederation = FFederation;
                TEventEntry Event;
                if (Connected && (OldFederation != "")) {
                    // unpublish and unsubscribe all
                    for (var i = 0; i < FEventEntryList.Count; i++) {
                        var EventName = FEventEntryList.GetEventName(i);
                        if ((EventName != "") && EventName.StartsWith(OldFederation + ".")) {
                            Event = FEventEntryList.GetEventEntry(i);
                            if (Event.IsSubscribed)
                                Event.UnSubscribe(false);
                            if (Event.IsPublished)
                                Event.UnPublish(false);
                        }
                    }
                }
                FFederation = value;
                if (Connected && (OldFederation != "")) {
                    // publish and subscribe all
                    for (var i = 0; i < FEventEntryList.Count; i++) {
                        var EventName = FEventEntryList.GetEventName(i);
                        if ((EventName != "") && EventName.StartsWith(OldFederation + ".")) {
                            Event = FEventEntryList.GetEventEntry(i);
                            Event.FEventName = FFederation + Event.EventName.Remove(0, OldFederation.Length);
                            if (Event.IsSubscribed)
                                Event.Subscribe();
                            if (Event.IsPublished)
                                Event.Publish();
                        }
                    }
                }
            }
        }

        public string RemoteHost {
            get { return FRemoteHost; }
        }

        public int RemotePort {
            get { return FRemotePort; }
        }

        public Int32 OwnerID {
            get { return FOwnerID; }
            set {
                if (FOwnerID != value) {
                    FOwnerID = value;
                    SetOwner();
                }
            }
        }

        public string OwnerName {
            get { return FOwnerName; }
            set {
                if (FOwnerName != value) {
                    FOwnerName = value;
                    SetOwner();
                }
            }
        }

        public Int32 UniqueClientID {
            get { return GetUniqueClientID(); }
        }

        public Int32 ClientHandle {
            get { return FClientHandle; }
        }

        ~TConnection() {
            Close();
        }

        public static string ConvertToHex(byte[] aBuffer) {
            var hex = "";
            foreach (var b in aBuffer) {
                hex += string.Format("{0:x2}", (uint) Convert.ToUInt32(b.ToString()));
            }
            return hex;
        }

        public static string ConvertEscapes(byte[] aBuffer) {
            var hex = "";
            foreach (var b in aBuffer) {
                if ((' ' <= b && b <= 'Z') || ('a' <= b && b <= 'z'))
                    hex += (char) b;
                else
                    hex += '<' + string.Format("{0:x2}", (uint) Convert.ToUInt32(b.ToString())) + '>';
            }
            return hex;
        }

        private TEventEntry EventIDToEventL(Int32 aEventID) {
            lock (FEventEntryList) {
                return FEventEntryList.GetEventEntry(aEventID);
            }
        }

        private TEventEntry AddEvent(string aEventName) {
            TEventEntry Event;
            var EventID = 0;
            while (EventID < FEventEntryList.Count && !FEventEntryList.GetEventEntry(EventID).IsEmpty)
                EventID += 1;
            if (EventID < FEventEntryList.Count) {
                Event = FEventEntryList.GetEventEntry(EventID);
                Event.FEventName = aEventName;
                Event.FParent = null;
            }
            else
                Event = FEventEntryList.AddEvent(this, aEventName);
            return Event;
        }

        private TEventEntry AddEventL(string aEventName) {
            lock (FEventEntryList) {
                return AddEvent(aEventName);
            }
        }

        private TEventEntry FindOrAddEventL(string aEventName) {
            lock (FEventEntryList) {
                var Event = FEventEntryList.EventEntryOnName(aEventName);
                if (Event == null)
                    Event = AddEvent(aEventName);
                return Event;
            }
        }

        private TEventEntry FindEventL(string aEventName) {
            lock (FEventEntryList) {
                return FEventEntryList.EventEntryOnName(aEventName);
            }
        }

        private TEventEntry FindEventParentL(string aEventName) {
            lock (FEventEntryList) {
                TEventEntry ParentEvent = null;
                var ParentEventNameLength = -1;
                string EventName;
                for (var EventID = 0; EventID < FEventEntryList.Count; EventID++) {
                    EventName = FEventEntryList.GetEventEntry(EventID).EventName;
                    if (EventName.EndsWith(EventFilterPostFix) &&
                        aEventName.StartsWith(EventName.Substring(0, EventName.Length - 1))) {
                        if (ParentEventNameLength < EventName.Length) {
                            ParentEvent = FEventEntryList.GetEventEntry(EventID);
                            ParentEventNameLength = EventName.Length;
                        }
                    }
                }
                return ParentEvent;
            }
        }

        private TEventEntry FindEventAutoPublishL(string aEventName) {
            var Event = FindEventL(aEventName);
            if (Event == null && AutoPublish)
                Event = Publish(aEventName, false);
            return Event;
        }

        internal int WriteCommand(TCommands aCommand, byte[] aPayload) {
            lock (WriteCommandLock) {
                var Buffer = new TByteBuffer();

                Buffer.Prepare(MagicBytes);
                Buffer.Prepare((Int32) aCommand);
                Buffer.Prepare((Int32) 0); // payload size
                if ((aPayload != null) && (aPayload.Length > 0)) {
                    Buffer.Prepare(aPayload);
                    Buffer.Prepare(CheckStringMagic);
                }
                Buffer.PrepareApply();
                Buffer.QWrite(MagicBytes);
                Buffer.QWrite((Int32) aCommand);
                if ((aPayload != null) && (aPayload.Length > 0)) {
                    Buffer.QWrite((Int32) aPayload.Length);
                    Buffer.QWrite(aPayload);
                    Buffer.QWrite(CheckStringMagic);
                }
                else
                    Buffer.QWrite((Int32) 0);
                // send buffer over socket
                if (Connected) {
                    try {
                        WriteCommandLow(Buffer.Buffer, Buffer.Length);
                        return Buffer.Length;
                    }
                    catch {
                        Close();
                        return iceConnectionClosed;
                    }
                }
                else
                    return iceConnectionClosed;
            }
        }

        private string PrefixFederation(string aName, bool aUseFederationPrefix = true) {
            if (FFederation != "" && aUseFederationPrefix)
                return FFederation + "." + aName;
            else
                return aName;
        }

        // command handlers
        protected override void HandleCommand(TCommands aCommand, TByteBuffer aPayload) {
            switch (aCommand) {
                case TCommands.icEvent:
                    HandleCommandEvent(aPayload);
                    break;
                case TCommands.icSetVariable:
                    HandleCommandVariable(aPayload);
                    break;
                case TCommands.icSetEventIDTranslation:
                    FEventTranslation.SetEventTranslation(
                        aPayload.PeekInt32(0, TEventTranslation.InvalidTranslatedEventID),
                        aPayload.PeekInt32(sizeof (Int32), TEventTranslation.InvalidTranslatedEventID));
                    break;
                case TCommands.icUniqueClientID:
                    aPayload.Read(out FUniqueClientID);
                    aPayload.Read(out FClientHandle);
                    break;
                case TCommands.icTimeStamp:
                    // ignore for now, only when using and syncing local time (we trust hub time for now)
                    aPayload.Read(out FBrokerAbsoluteTime);
                    aPayload.Read(out FBrokerTick);
                    aPayload.Read(out FBrokerTickDelta);
                    break;
                case TCommands.icEventNames:
                    HandleEventNames(aPayload);
                    break;
                case TCommands.icEndSession:
                    Close();
                    break;
                case TCommands.icSubscribe:
                case TCommands.icPublish:
                case TCommands.icUnsubscribe:
                case TCommands.icUnpublish:
                    HandleSubAndPub(aCommand, aPayload);
                    break;
                default:
                    HandleCommandOther(aCommand, aPayload);
                    break;
            }
        }

        private void HandleCommandEvent(TByteBuffer aPayload) {
            var txEventID = FEventTranslation.TranslateEventID(aPayload.ReadInt32());
            if (txEventID != TEventTranslation.InvalidTranslatedEventID)
                EventIDToEventL(txEventID).HandleEvent(aPayload);
            //else 
            //Log.WriteLn("Invalid event id found in event from "+FRemoteHost, llError, 1);
        }

        private void HandleCommandVariable(TByteBuffer aPayload) {
            if (FOnVariable == null && FOnStatusUpdate == null) return;
            var varName = aPayload.ReadString();
            // check if it is a status update
            if (varName.EndsWith(msVarSepChar + ModelStatusVarName, StringComparison.OrdinalIgnoreCase)) {
                varName.Remove(varName.Length - (msVarSepChar.Length + ModelStatusVarName.Length));
                var modelName = varName.Substring(8, varName.Length - 8);
                var modelUniqueClientID = varName.Substring(0, 8);
                aPayload.ReadInt32();
                var status = aPayload.ReadInt32(-1);
                var progress = aPayload.ReadInt32(-1);
                FOnStatusUpdate(this, modelUniqueClientID, modelName, progress, status);
            }
            else {
                var varValue = aPayload.ReadByteBuffer();
                var prevValue = new TByteBuffer();
                FOnVariable(this, varName, varValue.Buffer, prevValue.Buffer);
            }
        }

        private void HandleEventNames(TByteBuffer aPayload) {
            if (OnEventNames == null) return;
            Int32 ec;
            aPayload.Read(out ec);
            var eventNames = new TEventNameEntry[ec];
            for (var en = 0; en < eventNames.Length; en++) {
                eventNames[en] = new TEventNameEntry {
                    EventName = aPayload.ReadString(),
                    Publishers = aPayload.ReadInt32(),
                    Subscribers = aPayload.ReadInt32(),
                    Timers = aPayload.ReadInt32()
                };
            }
            OnEventNames(this, eventNames);
        }

        private void HandleSubAndPub(TCommands aCommand, TByteBuffer aPayload) {
            string eventName;
            TEventEntry ee;
            switch (aCommand) {
                case TCommands.icSubscribe:
                case TCommands.icPublish:
                    Int32 eventID;
                    aPayload.Read(out eventID);
                    Int32 eventEntryType;
                    aPayload.Read(out eventEntryType);
                    aPayload.Read(out eventName);
                    ee = FindEventL(eventName);
                    if (ee == null) {
                        var ep = FindEventParentL(eventName);
                        if (ep != null) {
                            ee = AddEventL(eventName);
                            ee.FParent = ep;
                            ee.CopyHandlersFrom(ep);
                        }
                    }
                    else {
                        if (OnSubAndPub != null && !ee.IsEmpty)
                            OnSubAndPub(this, aCommand, eventName);
                    }
                    if (ee != null)
                        ee.HandleOnSubAndPub(aCommand);
                    break;
                case TCommands.icUnsubscribe:
                case TCommands.icUnpublish:
                    aPayload.Read(out eventName);
                    if (OnSubAndPub != null)
                        OnSubAndPub(this, aCommand, eventName);
                    ee = FindEventL(eventName);
                    if (ee != null)
                        ee.HandleOnSubAndPub(aCommand);
                    break;
            }
        }

        private void HandleCommandOther(TCommands aCommand, TByteBuffer aPayload) {
            // override to implement protocol extensions
        }

        private int RequestUniqueClientID() {
            var payload = new TByteBuffer();
            payload.Prepare((Int32) 0);
            payload.Prepare((Int32) 0);
            payload.PrepareApply();
            payload.QWrite((Int32) 0);
            payload.QWrite((Int32) 0);
            return WriteCommand(TCommands.icUniqueClientID, payload.Buffer);
        }

        private int SetOwner() {
            if (Connected) {
                var Payload = new TByteBuffer();
                Payload.Prepare(FOwnerID);
                Payload.Prepare(FOwnerName);
                Payload.PrepareApply();
                Payload.QWrite(FOwnerID);
                Payload.QWrite(FOwnerName);
                return WriteCommand(TCommands.icSetClientInfo, Payload.Buffer);
            }
            else
                return iceConnectionClosed;
        }

        public bool Open(string aHost, int aPort, bool aStartReadingThread = true) {
            Close();
            try {
                FRemoteHost = aHost;
                FRemotePort = aPort;
                OpenLow(FRemoteHost, FRemotePort, 1000);
                if (aStartReadingThread && Connected) {
                    FThread = new Thread(ReadCommands);
                    FThread.Name = "IMB command reader";
                    FThread.Start();
                }
                if (Connected) {
                    if (FIMB2Compatible)
                        RequestUniqueClientID();
                    SetOwner();
                    // request all variables if delegates defined
                    if (FOnVariable != null || FOnStatusUpdate != null)
                        WriteCommand(TCommands.icAllVariables, null);
                }
                return Connected;
            }
            catch (Exception e) {
                return false;
            }
        }

        public void Close() {
            if (Connected) {
                if (OnDisconnect != null)
                    OnDisconnect(this);
                WriteCommand(TCommands.icEndSession, null);
                CloseLow();
                FThread = null;
            }
        }

        public event TOnDisconnect OnDisconnect;

        public event TOnSubAndPub OnSubAndPub;

        public void SetThrottle(Int32 aThrottle) {
            var Payload = new TByteBuffer();
            Payload.Prepare(aThrottle);
            Payload.PrepareApply();
            Payload.QWrite(aThrottle);
            WriteCommand(TCommands.icSetThrottle, Payload.Buffer);
        }

        public void SetState(TConnectionState aState) {
            var Payload = new TByteBuffer();
            Payload.Prepare((Int32) aState);
            Payload.PrepareApply();
            Payload.QWrite((Int32) aState);
            WriteCommand(TCommands.icSetState, Payload.Buffer);
        }

        // owner

        // subscribe/publish
        public TEventEntry Subscribe(string aEventName, bool aUseFederationPrefix = true) {
            var Event = FindOrAddEventL(PrefixFederation(aEventName, aUseFederationPrefix));
            if (!Event.IsSubscribed)
                Event.Subscribe();
            return Event;
        }

        public TEventEntry Publish(string aEventName, bool aUseFederationPrefix = true) {
            var Event = FindOrAddEventL(PrefixFederation(aEventName, aUseFederationPrefix));
            if (!Event.IsPublished)
                Event.Publish();
            return Event;
        }

        public void UnSubscribe(string aEventName, bool aUseFederationPrefix = true) {
            var Event = FindEventL(PrefixFederation(aEventName, aUseFederationPrefix));
            if (Event != null && Event.IsSubscribed)
                Event.UnSubscribe();
        }

        public void UnPublish(string aEventName, bool aUseFederationPrefix = true) {
            var Event = FindEventL(PrefixFederation(aEventName, aUseFederationPrefix));
            if (Event != null && Event.IsPublished)
                Event.UnPublish();
        }

        public int SignalEvent(string aEventName, TEventEntry.TEventKind aEventKind, TByteBuffer aEventPayload,
            bool aUseFederationPrefix = true) {
            var Event = FindEventAutoPublishL(PrefixFederation(aEventName, aUseFederationPrefix));
            if (Event != null)
                return Event.SignalEvent(aEventKind, aEventPayload.Buffer);
            else
                return iceNotEventPublished;
        }

        public int SignalEvent(int aEventID, TEventEntry.TEventKind aEventKind, TByteBuffer aEventPayload,
            bool aUseFederationPrefix = true) {
            return EventIDToEventL(aEventID).SignalEvent(aEventKind, aEventPayload.Buffer);
        }

        public int SignalBuffer(string aEventName, Int32 aBufferID, byte[] aBuffer, Int32 aEventFlags = 0,
            bool aUseFederationPrefix = true) {
            var Event = FindEventAutoPublishL(PrefixFederation(aEventName, aUseFederationPrefix));
            if (Event != null)
                return Event.SignalBuffer(aBufferID, aBuffer, aEventFlags);
            else
                return iceNotEventPublished;
        }

        public int SignalBuffer(int aEventID, Int32 aBufferID, byte[] aBuffer, Int32 aEventFlags = 0) {
            return EventIDToEventL(aEventID).SignalBuffer(aBufferID, aBuffer, aEventFlags);
        }

        public int SignalChangeObject(string aEventName, Int32 aAction, Int32 aObjectID, string aAttribute = "",
            bool aUseFederationPrefix = true) {
            var Event = FindEventAutoPublishL(PrefixFederation(aEventName, aUseFederationPrefix));
            if (Event != null)
                return Event.SignalChangeObject(aAction, aObjectID, aAttribute);
            else
                return iceNotEventPublished;
        }

        public int SignalChangeObject(int aEventID, Int32 aAction, Int32 aObjectID, string aAttribute = "") {
            return EventIDToEventL(aEventID).SignalChangeObject(aAction, aObjectID, aAttribute);
        }

        public int SignalStream(string aEventName, string aStreamName, Stream aStream, bool aUseFederationPrefix = true) {
            var Event = FindEventAutoPublishL(PrefixFederation(aEventName, aUseFederationPrefix));
            if (Event != null)
                return Event.SignalStream(aStreamName, aStream);
            else
                return iceNotEventPublished;
        }

        public int SignalStream(int aEventID, string aStreamName, Stream aStream) {
            return EventIDToEventL(aEventID).SignalStream(aStreamName, aStream);
        }

        // variables

        private event TOnVariable FOnVariable;

        public event TOnVariable OnVariable {
            add {
                FOnVariable += value;
                WriteCommand(TCommands.icAllVariables, null); // request all varibales for initial values
            }
            remove { FOnVariable -= value; }
        }

        public void SetVariableValue(string aVarName, string aVarValue) {
            var payload = new TByteBuffer();
            payload.Prepare(aVarName);
            payload.Prepare(aVarValue);
            payload.PrepareApply();
            payload.QWrite(aVarName);
            payload.QWrite(aVarValue);
            WriteCommand(TCommands.icSetVariable, payload.Buffer);
        }

        public void SetVariableValue(string aVarName, TByteBuffer aVarValue) {
            var payload = new TByteBuffer();
            payload.Prepare(aVarName);
            payload.Prepare(aVarValue);
            payload.PrepareApply();
            payload.QWrite(aVarName);
            payload.QWrite(aVarValue);
            WriteCommand(TCommands.icSetVariable, payload.Buffer);
        }

        public void SetVariableValue(string aVarName, string aVarValue, TVarPrefix aVarPrefix) {
            var payload = new TByteBuffer();
            payload.Prepare((Int32) aVarPrefix);
            payload.Prepare(aVarName);
            payload.Prepare(aVarValue);
            payload.PrepareApply();
            payload.QWrite((Int32) aVarPrefix);
            payload.QWrite(aVarName);
            payload.QWrite(aVarValue);
            WriteCommand(TCommands.icSetVariablePrefixed, payload.Buffer);
        }

        public void SetVariableValue(string aVarName, TByteBuffer aVarValue, TVarPrefix aVarPrefix) {
            var payload = new TByteBuffer();
            payload.Prepare((Int32) aVarPrefix);
            payload.Prepare(aVarName);
            payload.Prepare(aVarValue);
            payload.PrepareApply();
            payload.QWrite((Int32) aVarPrefix);
            payload.QWrite(aVarName);
            payload.QWrite(aVarValue);
            WriteCommand(TCommands.icSetVariablePrefixed, payload.Buffer);
        }

        private event TOnStatusUpdate FOnStatusUpdate;

        public event TOnStatusUpdate OnStatusUpdate {
            add {
                FOnStatusUpdate += value;
                WriteCommand(TCommands.icAllVariables, null); // request all varibales for initial values
            }
            remove { FOnStatusUpdate -= value; }
        }

        private int GetUniqueClientID() {
            if (FUniqueClientID != 0) return FUniqueClientID;
            RequestUniqueClientID();
            var spinCount = 10; // 10*500 ms
            while (FUniqueClientID == 0 && spinCount > 0) {
                Thread.Sleep(500);
                spinCount--;
            }
            if (FUniqueClientID == 0)
                FUniqueClientID = -1;
            return FUniqueClientID;
        }

        // status for UpdateStatus

        public void UpdateStatus(Int32 aProgress, Int32 aStatus) {
            var payload = new TByteBuffer();
            payload.Prepare(aStatus);
            payload.Prepare(aProgress);
            payload.PrepareApply();
            payload.QWrite(aStatus);
            payload.QWrite(aProgress);
            if (FIMB2Compatible)
                SetVariableValue(
                    UniqueClientID.ToString("X8") + PrefixFederation(OwnerName).ToUpper() + msVarSepChar +
                    ModelStatusVarName, payload);
            else
                SetVariableValue(PrefixFederation(OwnerName).ToUpper() + msVarSepChar + ModelStatusVarName, payload,
                    TVarPrefix.vpUniqueClientID);
        }

        public void RemoveStatus() {
            if (FIMB2Compatible)
                SetVariableValue(
                    UniqueClientID.ToString("X8") + PrefixFederation(OwnerName) + msVarSepChar + ModelStatusVarName, "");
            else
                SetVariableValue(PrefixFederation(OwnerName) + msVarSepChar + ModelStatusVarName, "",
                    TVarPrefix.vpUniqueClientID);
        }

        public event TEventEntry.TOnFocus OnFocus {
            add {
                FFocusEvent = Subscribe(FocusEventName);
                FFocusEvent.OnFocus += value;
            }
            remove {
                if (FFocusEvent != null)
                    FFocusEvent.OnFocus -= value;
            }
        }

        public int SignalFocus(double aX, double aY) {
            if (FFocusEvent == null)
                FFocusEvent = FindEventAutoPublishL(PrefixFederation(FocusEventName));
            if (FFocusEvent != null) {
                var payload = new TByteBuffer();
                payload.Prepare(aX);
                payload.Prepare(aY);
                payload.PrepareApply();
                payload.QWrite(aX);
                payload.QWrite(aY);
                return FFocusEvent.SignalEvent(TEventEntry.TEventKind.ekChangeObjectEvent, payload.Buffer);
            }
            else
                return iceNotEventPublished;
        }

        // imb 2 change federation

        public event TEventEntry.TOnChangeFederation OnChangeFederation {
            add {
                FChangeFederationEvent = Subscribe(FederationChangeEventName);
                FChangeFederationEvent.OnChangeFederation += value;
            }
            remove {
                if (FChangeFederationEvent != null)
                    FChangeFederationEvent.OnChangeFederation -= value;
            }
        }

        public int SignalChangeFederation(Int32 aNewFederationID, string aNewFederation) {
            if (FChangeFederationEvent == null)
                FChangeFederationEvent = FindEventAutoPublishL(PrefixFederation(FederationChangeEventName));
            return FChangeFederationEvent != null
                ? FChangeFederationEvent.SignalChangeObject(TEventEntry.actionChange, aNewFederationID, aNewFederation)
                : iceNotEventPublished;
        }

        // log
        public int LogWriteLn(string aLogEventName, string aLine, TEventEntry.TLogLevel aLevel) {
            if (FLogEvent == null)
                FLogEvent = FindEventAutoPublishL(PrefixFederation(aLogEventName));
            return FLogEvent != null
                ? FLogEvent.LogWriteLn(aLine, aLevel)
                : iceNotEventPublished;
        }

        // remote event info

        public event TOnEventnames OnEventNames;

        public int RequestEventname(string aEventNameFilter, Int32 aEventFilters) {
            var payload = new TByteBuffer();
            payload.Prepare(aEventNameFilter);
            payload.Prepare(aEventFilters);
            payload.PrepareApply();
            payload.QWrite(aEventNameFilter);
            payload.QWrite(aEventFilters);
            return WriteCommand(TCommands.icRequestEventNames, payload.Buffer);
        }

        public int SignalString(string eventName, string message) {
            var smallTestEventPayload = new TByteBuffer();
            // build payload for small event
            smallTestEventPayload.Prepare(message);
            smallTestEventPayload.PrepareApply();
            smallTestEventPayload.QWrite(message);
            return SignalEvent(eventName, TEventEntry.TEventKind.ekNormalEvent, smallTestEventPayload);
        }

        public int SignalIntString(string eventName, int cmd, string message)
        {
            var smallTestEventPayload = new TByteBuffer();
            // build payload for small event
            smallTestEventPayload.Prepare(cmd);
            smallTestEventPayload.Prepare(message);
            smallTestEventPayload.PrepareApply();
            smallTestEventPayload.QWrite(cmd);
            smallTestEventPayload.QWrite(message);
            return SignalEvent(eventName, TEventEntry.TEventKind.ekNormalEvent, smallTestEventPayload);
        }

        internal class TEventEntryList
        {
            private readonly Int32 FInitialSize;
            private Int32 FCount = 0;
            private TEventEntry[] FEvents = new TEventEntry[0];

            public TEventEntryList(Int32 aInitialSize = 8) {
                FInitialSize = aInitialSize;
                FCount = 0;
            }

            public Int32 Count {
                get { return FCount; }
            }

            public TEventEntry GetEventEntry(Int32 aEventID) {
                if (0 <= aEventID && aEventID < FCount)
                    return FEvents[aEventID];
                else
                    return null;
            }

            public string GetEventName(Int32 aEventID) {
                if (0 <= aEventID && aEventID < FCount) {
                    if (FEvents[aEventID] != null)
                        return FEvents[aEventID].EventName;
                    else
                        return null;
                }
                else
                    return "";
            }

            public void SetEventName(Int32 aEventID, string aEventName) {
                if (0 <= aEventID && aEventID < FCount) {
                    if (FEvents[aEventID] != null)
                        FEvents[aEventID].FEventName = aEventName;
                }
            }

            //public string EventName[Int32 aEventID]

            public TEventEntry AddEvent(TConnection aConnection, string aEventName) {
                FCount++;
                while (FCount > FEvents.Length) {
                    if (FEvents.Length == 0)
                        Array.Resize(ref FEvents, FInitialSize);
                    else
                        Array.Resize(ref FEvents, FEvents.Length*2);
                }
                FEvents[FCount - 1] = new TEventEntry(aConnection, FCount - 1, aEventName);

                return FEvents[FCount - 1];
            }

            public Int32 IndexOfEventName(string aEventName) {
                var i = FCount - 1;
                while (i >= 0 && GetEventName(i) != aEventName)
                    i--;
                return i;
            }

            public TEventEntry EventEntryOnName(string aEventName) {
                var i = FCount - 1;
                while (i >= 0 && GetEventName(i) != aEventName)
                    i--;
                if (i >= 0)
                    return FEvents[i];
                else
                    return null;
            }
        }

        internal class TEventTranslation
        {
            public const Int32 InvalidTranslatedEventID = -1;

            private Int32[] FEventTranslation;

            public TEventTranslation() {
                FEventTranslation = new Int32[32];
                // mark all entries as invalid
                for (var i = 0; i < FEventTranslation.Length; i++)
                    FEventTranslation[i] = InvalidTranslatedEventID;
            }

            public Int32 TranslateEventID(Int32 aRxEventID) {
                if ((0 <= aRxEventID) && (aRxEventID < FEventTranslation.Length))
                    return FEventTranslation[aRxEventID];
                else
                    return InvalidTranslatedEventID;
            }

            public void SetEventTranslation(Int32 aRxEventID, Int32 aTxEventID) {
                if (aRxEventID >= 0) {
                    // grow event translation list until it can contain the requested id
                    while (aRxEventID >= FEventTranslation.Length) {
                        var FormerSize = FEventTranslation.Length;
                        // resize event translation array to double the size
                        Array.Resize(ref FEventTranslation, FEventTranslation.Length*2);
                        // mark all new entries as invalid
                        for (var i = FormerSize; i < FEventTranslation.Length; i++)
                            FEventTranslation[i] = InvalidTranslatedEventID;
                    }
                    FEventTranslation[aRxEventID] = aTxEventID;
                }
            }

            public void ResetEventTranslation(Int32 aTxEventID) {
                for (var i = 0; i < FEventTranslation.Length; i++) {
                    if (FEventTranslation[i] == aTxEventID)
                        FEventTranslation[i] = InvalidTranslatedEventID;
                }
            }
        }
    }
}