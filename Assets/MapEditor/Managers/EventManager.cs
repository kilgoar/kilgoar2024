using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public static class EventManager
{
    public static readonly InterfaceEvent interfaceEvent = new InterfaceEvent();
	
	public class InterfaceEvent {
		public UnityAction<Component, int> ButtonClick;
	}
	
}
