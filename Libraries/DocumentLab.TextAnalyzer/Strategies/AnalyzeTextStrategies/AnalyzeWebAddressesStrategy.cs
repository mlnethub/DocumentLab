﻿namespace DocumentLab.TextAnalyzer.Strategies.AnalyzeTextStrategies
{
  using System;
  using System.Collections.Generic;
  using DocumentLab.TextAnalyzer.Interfaces;
  using System.Linq;
  using System.Text.RegularExpressions;
  using DocumentLab.TextAnalyzer.Utilities;
  using DocumentLab.Contracts;
  using DocumentLab.Contracts.Enums.Types;
  using DocumentLab.Core.Storage;

  public class AnalyzeWebAddressesStrategy : IAnalyzeTextStrategy
  {
    IEnumerable<AnalyzedText> IAnalyzeTextStrategy.Analyze(OcrResult ocrResult)
    {
      var possibleWebAddresses =
        string.Join(" ", ocrResult.Result)
        .Split(' ')
        .Select(x => x.Trim())
        .Where(x => !string.IsNullOrWhiteSpace(x))
        .Select(x => TryParseAndFixWebAddress(x));

      return possibleWebAddresses
        .Select(x => new AnalyzedText()
        {
          Text = x,
          TextType = TextType.WebAddress.ToString(),
          BoundingBox = ocrResult.BoundingBox
        })
        .Where(x => !string.IsNullOrWhiteSpace(x.Text));
    }

    public static string TryParseAndFixWebAddress(string webAddress)
    {
      if (!Uri.IsWellFormedUriString(webAddress, UriKind.RelativeOrAbsolute))
      {
        if (!Regex.IsMatch(webAddress, Constants.TextAnalysisConfiguration.BadOcrWebAddressRegex))
          return string.Empty;

        for (int i = 2; i < 4; i++)
        {
          var webAddressTopLevelDomain = new string(
            webAddress
              .Reverse()
              .Take(i)
              .Reverse()
              .ToArray()
            );

          if (FileReader.GetFileLines(Constants.TopLevelDomainListPath).Contains(webAddressTopLevelDomain.ToUpper()))
          {
            webAddress = webAddress.Insert(webAddress.IndexOf(webAddressTopLevelDomain), ".");
            break;
          }
        }
      }

      if(Regex.IsMatch(webAddress, Constants.TextAnalysisConfiguration.WebAddressRegex))
      {
        try
        {
          var webAddressUri = new UriBuilder(webAddress).Uri;
          if (Constants.ValidateWebAddresses)
          {
            return HttpClientHelpers.IsWebsiteAvailable(webAddressUri)
              ? webAddress
              : string.Empty;
          }

          return webAddressUri.AbsoluteUri;
        }
        catch (UriFormatException)
        {
          return string.Empty;
        }
      }

      return string.Empty;
    }
  }
}
