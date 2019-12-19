using System.Buffers;
using System.Text;

namespace Perper.WebJobs.Extensions
{
    public static class PerperReadonlySequenceExtensions
    {
        public static string ToAsciiString(this ReadOnlySequence<byte> buffer)         
        {                                                                              
            if (buffer.IsSingleSegment)                                                
            {                                                                          
                return Encoding.ASCII.GetString(buffer.First.Span);                    
            }                                                                          
                                                                               
            return string.Create((int) buffer.Length, buffer, (span, sequence) =>      
            {                                                                          
                foreach (var segment in sequence)                                      
                {                                                                      
                    Encoding.ASCII.GetChars(segment.Span, span);                       
                                                                               
                    span = span.Slice(segment.Length);                                 
                }                                                                      
            });                                                                        
        }                                                                              
    }
}