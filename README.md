# Clinical Summary Generator

**Clinical Summary Generator** is an AI-powered web application that analyzes unstructured clinical notes and generates clear, concise summaries. It can also extract structured clinical data such as diagnoses, medications, and plan items. The app is designed to help clinicians, developers, and healthcare stakeholders make unstructured documentation more usable.

https://github.com/user-attachments/assets/a91cdeae-41a6-469b-9ee1-899dfb203b6e

https://github.com/user-attachments/assets/3647ea29-ff2c-4e9e-8936-02b5cc1506b7

## Features

- Accepts pasted text or uploaded `.txt` and `.docx` clinical notes
- Generates concise summaries using Azure OpenAI
- Extracts structured fields (Diagnoses, Medications, Plan)
- Copy-to-clipboard functionality
- Local file caching (during development) to avoid redundant GPT calls
- Built with ASP.NET Razor Pages and .NET 8
- Deployable to Azure App Service

## Summary Styles

Users can select from five summary styles:

- **Brief** – 3–5 key bullet points
- **Detailed** – Narrative summary
- **SOAP** – Structured output in Subjective, Objective, Assessment, Plan format
- **Structured Extraction** – Displays extracted clinical data in labeled sections
- **Structured (JSON)** – Returns the same extracted data in JSON format

## File Upload Support

Users can upload `.txt` or `.docx` clinical notes. The application extracts readable text and populates the form automatically, overwriting any manual input.

- `.txt` files are read as-is
- `.docx` files are parsed using Open XML SDK, with formatting stripped

## Use Cases

- Assisting clinicians with documentation
- Normalizing narrative text into structured records
- Rapid EHR data entry prototyping
- AI prompt testing with medical content

## Developer Notes

- GPT calls are made using Azure OpenAI’s chat completion endpoint
- The system prompt and summary styles are stored in environment variables for flexibility
- Local development uses in-memory caching
- The application is designed to run both locally and in Azure

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
