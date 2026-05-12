using System;
using System.Collections.Generic;
using System.Text;

namespace NecroPanel.ApplicationN.Models
{
    public class ArquivoItemModel
    {
        public string Nome { get; set; }

        public string Caminho { get; set; }

        public bool IsDiretorio { get; set; }

        public string TipoDescricao =>
            IsDiretorio
                ? "Pasta"
                : "Arquivo";

        public string Icone
        {
            get
            {
                if (IsDiretorio)
                    return "folder.png";

                if (Nome == "docker-compose.yml")
                    return "docker_file.png";

                if (Nome.EndsWith(".txt")
                    || Nome.EndsWith(".json")
                    || Nome.EndsWith(".xml")
                    || Nome.EndsWith(".yml")
                    || Nome.EndsWith(".yaml")
                    || Nome.EndsWith(".cs")
                    || Nome.EndsWith(".js"))
                    return "text_file.png";

                return "file.png";
            }
        }
    }
}
