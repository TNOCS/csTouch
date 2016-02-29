using System;
using System.Xml.Linq;
using ProtoBuf;

namespace DataServer
{
    public enum TaskState
    {
        None,
        Open,
        Inprogress,
        Finished
    }

    [ProtoContract]
    public class Task : BaseContent
    {

        public override string XmlNodeId
        {
            get
            {
                return "Task";
            }
        }

        private string title;

        [ProtoMember(1)]
        public string Title
        {
            get { return title; }
            set { title = value; NotifyOfPropertyChange(()=>Title); }
        }

        private string description;

        [ProtoMember(5)]
        public string Description
        {
            get { return description; }
            set { description = value; NotifyOfPropertyChange(() => Description); }
        }

        private string taskId;
        [ProtoMember(2)]
        public string TaskId
        {
            get { return taskId; }
            set { taskId = value; NotifyOfPropertyChange(()=>TaskId); }
        }


        private TaskState state;
        [ProtoMember(3)]
        public TaskState State
        {
            get { return state; }
            set {

                if (state == value) return;
                state = value;             
                if (state == TaskState.Finished) CompletedTime = DateTime.Now;
                NotifyOfPropertyChange(()=>State); 
                NotifyOfPropertyChange(()=>IsFinished);
            }
        }

        private DateTime completedTime;

        [ProtoMember(4)]
        public DateTime CompletedTime
        {
            get { return completedTime; }
            set { completedTime = value; NotifyOfPropertyChange(()=>CompletedTime); }
        }
        

        public bool? IsFinished
        {
            get
            {
                switch (State)
                {
                    case TaskState.Finished:
                        return true;
                    case TaskState.Open:
                        return false;
                    case TaskState.Inprogress:
                        return null;
                }
                return false;
            }
            set
            {
                if (value.HasValue)
                {
                    State = (value.Value) ? TaskState.Inprogress : TaskState.Finished; 
                }
                else
                {
                    State = TaskState.Open;
                }
            }
        }

        public override XElement ToXml(ServiceSettings settings)
        {
            var res = ToXmlBase(settings);
            res.SetAttributeValue("Title",Title);
            res.SetAttributeValue("TaskId",TaskId ?? string.Empty);
            res.SetAttributeValue("State",State);
            res.SetElementValue("Description",Description);

            //var res = new XElement(XmlNode,
            //                       new XAttribute("Id", Id),
            //                       new XAttribute("Title", Title),
            //                       new XAttribute("TaskId", TaskId ?? string.Empty),
            //                       new XAttribute("State", State),
            //                       new XElement("Description", Description),
            //                       new XElement("Labels",
            //                                    from label in Labels select new XElement(label.Key, label.Value)),
            //                       new XElement("AllMedia", 
            //                                    from media in AllMedia select new XElement("Media", 
            //                                        new XAttribute("Title", media.Title),
            //                                        new XAttribute("Id", media.Id))));
            //res.SetAttributeValue("Id", Id);
            //res.SetAttributeValue("Title", Title);
            //res.SetAttributeValue("TaskId", TaskId);
            //res.SetAttributeValue("State",State);
            //res.SetElementValue("Description", Description);
            //var labels = res.Element()
            return res;
        }

        public override void FromXml(XElement res, string directoryName)
        {
            FromXmlBase(ref res,directoryName);
            Title = res.GetString("Title");
            TaskId = res.GetString("TaskId");
            ContentId = TaskId;
            State = (TaskState)Enum.Parse(typeof(TaskState), res.GetString("State"));
            Description = res.Element("Description") == null 
                ? string.Empty 
                : res.Element("Description").Value;
        }
        
    }
}