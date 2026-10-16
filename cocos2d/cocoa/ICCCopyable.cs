using System;

namespace Cocos2D;

	public interface ICCCopyable
	{
		/// <summary>
		/// Copies this object. When <paramref name="zone"/> is given, the copy is made into it;
		/// otherwise a new instance is created.
		/// </summary>
		Object Copy(ICCCopyable? zone);
	}

