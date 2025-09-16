namespace SRAAI.Shared.Dtos.Summary;
public class SummaryDto
{

    public string Category { get; set; }   // पात्र, अपात्र, अनिर्णित, एकूण
    public int Nivasi { get; set; }        // निवासी
    public int Anivasi { get; set; }       // अनिवासी
    public int Samyukt { get; set; }       // संयुक्त
    public int Dharsthal { get; set; }     // धारस्थळ
    public int Total => Nivasi + Anivasi + Samyukt + Dharsthal; // auto calc
}
