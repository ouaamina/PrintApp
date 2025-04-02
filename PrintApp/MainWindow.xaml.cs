using Microsoft.Win32;
using PdfSharpLib = PdfSharp.Pdf; 
using PdfSharpContent = PdfSharp.Pdf.Content; 
using PdfSharpIO = PdfSharp.Pdf.IO; 
using PdfSharpObjects = PdfSharp.Pdf.Content.Objects; 
using System.Drawing.Printing;
using System.Windows;
using System.Text.RegularExpressions; 
using System.Windows.Controls;

namespace WpfApp1;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private string pdfFilePath;

    public MainWindow()
    {
        InitializeComponent();
        LoadPrinters();
    }

    private void OpenPdfFile(object sender, RoutedEventArgs e)
    {
        // Open file dialog to select PDF
        OpenFileDialog openFileDialog = new OpenFileDialog
        {
            Filter = "PDF files (*.pdf)|*.pdf",
            Title = "Select a PDF file"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            pdfFilePath = openFileDialog.FileName;
            PdfViewer.Navigate(new Uri(pdfFilePath));
        }
    }

    private void PrintPdfFile(object sender, RoutedEventArgs e)
    {
        if (PdfViewer.Source == null)
        {
            MessageBox.Show("Veuillez sélectionner un fichier PDF d'abord.");
            return;
        }

        string pdfFilePath = PdfViewer.Source.LocalPath;

        try
        {
            // Use PdfSharp to load the PDF document
            using (PdfSharpLib.PdfDocument document = PdfSharpIO.PdfReader.Open(pdfFilePath, PdfSharpIO.PdfDocumentOpenMode.Import))
            {
                // Check if the PDF contains color
                bool isColor = IsPdfInColor(document);

                // Determine the selected printer based on color preference
                var selectedPrinter = isColor
                    ? ColorPrinterComboBox.SelectedItem as string
                    : PrinterComboBox.SelectedItem as string;

                if (string.IsNullOrEmpty(selectedPrinter))
                {
                    MessageBox.Show("Please select a printer.");
                }
                else if (!int.TryParse(CopiesTextBox.Text, out int numberOfCopies))
                {
                    MessageBox.Show("Please enter a valid number of copies.");
                }
                else
                {
                    // Determine the pages to print
                    string pages = PagesComboBox.SelectedItem.ToString() == "Personnalisé" ? CustomPagesTextBox.Text : "Tous";

                    // Print the PDF file
                    MessageBox.Show("Start Printing.");

                    PrintPdf(pdfFilePath, selectedPrinter, numberOfCopies, pages);
                }
            }
        }
        catch (PdfSharp.Pdf.IO.PdfReaderException ex)
        {
            MessageBox.Show($"Error reading PDF file: {ex.Message}");
            // Log detailed information for further analysis
        }
    }

    private void LoadPrinters()
    {
        foreach (string printer in PrinterSettings.InstalledPrinters)
        {
            PrinterComboBox.Items.Add(printer);
            ColorPrinterComboBox.Items.Add(printer);
        }

        if (PrinterComboBox.Items.Count > 0)
        {
            PrinterComboBox.SelectedIndex = 0;
            ColorPrinterComboBox.SelectedIndex = 0;
        }
    }

    private void PrintPdf(string pdfFilePath, string printerName, int numberOfCopies, string pages)
    {
        // Load the PDF document using PdfiumViewer
        using (var document = PdfiumViewer.PdfDocument.Load(pdfFilePath))
        {
            // Create a PrintDocument
            PrintDocument printDocument = new PrintDocument
            {
                PrinterSettings = new PrinterSettings
                {
                    PrinterName = printerName,
                    Copies = (short)numberOfCopies
                }
            };

            int currentPage = 0;
            int totalPages = document.PageCount;

            printDocument.PrintPage += (sender, e) =>
            {
                if (e.Graphics != null)
                {
                    // Render the current PDF page to an image
                    using (var image = document.Render(currentPage, e.PageBounds.Width, e.PageBounds.Height, true))
                    {
                        e.Graphics.DrawImage(image, e.PageBounds);
                    }

                    // Move to the next page
                    currentPage++;

                    // Check if there are more pages to print
                    e.HasMorePages = currentPage < totalPages;
                }
                else
                {
                    MessageBox.Show("Graphics object is null.");
                }
            };

            printDocument.Print();
        }
    }

    private bool IsPdfInColor(PdfSharpLib.PdfDocument document)
    {
        foreach (var page in document.Pages)
        {
            var content = PdfSharpContent.ContentReader.ReadContent(page);
            foreach (var item in content)
            {
                if (item is PdfSharpObjects.COperator op)
                {
                    // Vérifiez les opérateurs de couleur pour RGB (RG et rg) et CMYK (K et k)
                    if (op.OpCode.Name == "RG" || op.OpCode.Name == "rg" || op.OpCode.Name == "K" || op.OpCode.Name == "k")
                    {
                        // Vérifiez les valeurs des opérateurs de couleur
                        var operands = op.Operands;
                        if (operands.Count == 3 || operands.Count == 4)
                        {
                            foreach (var operand in operands)
                            {
                                if (operand is PdfSharpObjects.CReal realOperand && realOperand.Value > 0)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
        }
        return false;
    }


    private void CopiesTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
    {
        // Use a regular expression to check if the input is a number
        e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");
    }

    private void PagesComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (CustomPagesTextBox == null || PagesComboBox.SelectedItem == null)
            return;

        // Get the selected ComboBoxItem and retrieve its Content
        if (PagesComboBox.SelectedItem is ComboBoxItem selectedItem)
        {
            string selectedText = selectedItem.Content.ToString();

            CustomPagesTextBox.Visibility = selectedText == "Personnalisé"
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }

    
}

