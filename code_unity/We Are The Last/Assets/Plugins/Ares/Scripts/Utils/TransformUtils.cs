using UnityEngine;

namespace Ares {
	public static class TransformUtils {
		public static Transform FindChild(Transform parent, string name){
			foreach(Transform child in parent){
				if(child.name == name){
					return child;
				}
				else{
					Transform foundChild = FindChild(child, name);
					if(foundChild != null){
						return foundChild;
					}
				}
			}

			return null;
		}
	}
}

