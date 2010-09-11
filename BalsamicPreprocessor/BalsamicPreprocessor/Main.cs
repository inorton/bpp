using System;
using System.IO;
using System.Xml;
using System.Collections.Generic;

namespace BalsamicPreprocessor
{
	class MainClass
	{
		private static Bpp processor;
	
		public static void Main (string[] args)
		{
			processor = new Bpp();
			processor.LoadBmmlFile( args[0] );
			processor.ProcessBmml("output.bmml");
		}
	
	}
	
	public class CustomBmmlControl
	{
		public XmlNode XmlNode   { get; set; }
		public string  ControlId { get; set; }
		public string  CustomData { get; set; }
	}

	public class Bpp
	{
		XmlDocument doc;
	
		public Bpp()
		{
			doc = new XmlDocument();
		}
		
		
		public void LoadBmmlFile( string filename )
		{
			StreamReader sr = new StreamReader( filename );
			doc.LoadXml( sr.ReadToEnd() );
			sr.Close();
		}
		
		public XmlDocument ProcessBmml( string outfile )
		{
			IEnumerable<CustomBmmlControl> clist = FindCustomControls( doc );
			
			foreach ( CustomBmmlControl c in clist ){
				Console.WriteLine( c.ControlId );
				// try to load that file
				
				if ( !String.IsNullOrEmpty(c.CustomData) ){
				
					Bpp subproc = new Bpp();
					subproc.LoadBmmlFile( c.CustomData );
					XmlDocument cust = subproc.ProcessBmml(c.CustomData);
					
					// get actual content
					XmlNode docdata = cust.FirstChild.FirstChild.FirstChild;
					
					Console.WriteLine( c.XmlNode.Name );
					
					XmlNode replace = doc.ImportNode( docdata, true );
					
					c.XmlNode.ParentNode.ReplaceChild( replace, c.XmlNode );
					
					Console.Error.WriteLine( c.XmlNode.Name );
					
					// copy position/id/size from replaced control to new one
					foreach ( XmlAttribute a in c.XmlNode.Attributes ){
						
						switch( a.Name ){
						
							case "controlID":
							case "x":
							case "y":							
							case "isInGroup":
							Console.Error.WriteLine( "keep Name={0} Value={1}", a.Name, a.Value );
							replace.Attributes[a.Name].Value = a.Value;
							break;
						
							
							default:
							Console.Error.WriteLine( "ignore Name={0} Value={1}", a.Name, a.Value );
							break;
						
						}
					}

				}
			}
			
			
			if ( outfile != null ){
				StreamWriter sw = new StreamWriter( outfile );	
				sw.Write( doc.OuterXml );
				sw.Close();
			}
			return doc;
		}
		
		
		public IEnumerable<CustomBmmlControl> FindCustomControls( XmlNode parent )
		{
			List<CustomBmmlControl> clist = new List<CustomBmmlControl>();
			
			CustomBmmlControl c = null;
			
			XmlNode idNode = null;
			XmlNode dataNode = null;
			
			foreach ( XmlNode n in parent.ChildNodes ){
				if ( n.Name.Equals("customID") ){
					idNode = n;
				}
				if ( n.Name.Equals("customData") ){
					dataNode = n;
				}
			}
			
			if ( idNode != null ){
				if ( dataNode != null ){
					c = new CustomBmmlControl(){ ControlId = idNode.InnerText, CustomData = dataNode.InnerText };
					c.XmlNode = idNode.ParentNode.ParentNode;
					clist.Add( c );
				}
			} else {
				if ( parent.HasChildNodes ){
					foreach ( XmlNode n in parent.ChildNodes ){
						if ( (c == null) || (n != c.XmlNode) ){
							clist.AddRange( FindCustomControls( n ) );
						}
					}
				}
			}
			
			
			return clist;
		}
		
	}
}

