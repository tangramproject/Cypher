using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace Tangram.Address.Exceptions
{
	public class TangramException : Exception
	{
		public string ResourceReferenceProperty { get; set; }

		public TangramException()
		{
		}

		public TangramException(string message)
			: base(message)
		{
		}

		public TangramException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		protected TangramException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			ResourceReferenceProperty = info.GetString("ResourceReferenceProperty");
		}

		[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
				throw new ArgumentNullException("info");

			info.AddValue("ResourceReferenceProperty", ResourceReferenceProperty);

			base.GetObjectData(info, context);
		}
	}
}
