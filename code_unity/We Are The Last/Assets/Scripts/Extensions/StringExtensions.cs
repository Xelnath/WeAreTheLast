using System;
using System.Collections.Generic;
using System.Text;


	static class IEnumerableExts
	{
		public static string mkString<A>(
		  this IEnumerable<A> e, Action<StringBuilder> appendSeparator,
		  string start = null, string end = null
		)
		{
			var sb = new StringBuilder();
			if ( start != null ) sb.Append( start );
			var first = true;
			foreach ( var a in e )
			{
				if ( first ) first = false;
				else appendSeparator( sb );
				sb.Append( a );
			}
			if ( end != null ) sb.Append( end );
			return sb.ToString();
		}

		public static string mkString<A>(
		  this IEnumerable<A> e, string separator, string start = null, string end = null
		)
		{
			if ( separator.Contains( "\0" ) ) throwNullStringBuilderException();
			return e.mkString( sb => sb.Append( separator ), start, end );
		}

		static void throwNullStringBuilderException()
		{
			// var sb = new StringBuilder();
			// sb.Append("foo");
			// sb.Append('\0');
			// sb.Append("bar");
			// sb.ToString() == "foobar" // -> false
			// sb.ToString() == "foo" // -> true
			throw new Exception(
			  "Can't have null char in a separator due to a Mono runtime StringBuilder bug!"
			);
		}

		public static string mkStringEnum<A>(
		  this IEnumerable<A> e, string separator = ", ", string start = "[", string end = "]"
		) => e.mkString( separator, start, end );

	public static bool isNull<A>( A value ) where A : class =>
	  // This might seem to be overkill, but on the case of Transforms that
	  // have been destroyed, target == null will return false, whereas
	  // target.Equals(null) will return true.  Otherwise we don't really
	  // get the benefits of the nanny.
	  value == null || value.Equals( null );

	//public static Option<A> opt<A>( A value ) where A : class =>
	//  isNull( value ) ? Option<A>.None : new Option<A>( value );

}