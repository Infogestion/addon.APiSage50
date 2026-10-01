using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime;
using System.Threading.Tasks;

namespace S50APIService.Api
{
    /// <summary>
    /// Cuerpo de una respuesta que ya está en bloques de bytes. Tras enviar una respuesta grande libera la memoria.
    /// </summary>
    internal sealed class ContenidoBloques : HttpContent
    {
        private const long TamanoGrande = 16 * 1024 * 1024;

        private List<byte[]> _bloques;
        private readonly long _longitud;

        public ContenidoBloques(List<byte[]> bloques)
        {
            _bloques = bloques;
            _longitud = bloques.Sum(b => (long)b.Length);
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            foreach (var bloque in _bloques)
                await stream.WriteAsync(bloque, 0, bloque.Length);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = _longitud;
            return true;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _bloques != null)
            {
                _bloques = null;
                if (_longitud >= TamanoGrande)
                    Task.Run(() =>
                    {
                        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                        GC.Collect();
                    });
            }
            base.Dispose(disposing);
        }
    }
}
