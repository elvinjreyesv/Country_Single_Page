﻿using ERV.App.DataAccess.Repositories.Abstracts;
using ERV.App.Models.ViewModels.Public.Outputs;
using ERV.App.Services.Abstracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ERV.App.Infrastructure.Extensions;
using ERV.App.Models.ViewModels.Shared;
using ERV.App.Models.Enums;
using ERV.App.Models.Entities;

namespace ERV.App.Services
{
    public class CountryService: ICountryService
    {
        private readonly ICountryRepository _countryRepository;
        private readonly ISettingsService _settingsService;
        public CountryService(ISettingsService settingsService, ICountryRepository countryRepository)
        {
            _settingsService = settingsService;
            _countryRepository = countryRepository;
        }

        public async Task<List<CountryInfoDTO>> Countries()
        {
            try
            {
                var output = Enumerable.Empty<CountryInfoDTO>().ToList();
                var countries = await _countryRepository.GetCountries();

                if(countries !=null && countries.Any())
                {
                    output = countries.Select(row => new CountryInfoDTO()
                    {
                        Code = row.cca2.CleanSpace(),
                        Name = row.name.common,
                        Flag = row.flags.png,
                        Region = row.region,
                        SubRegion = row.subregion
                    }).ToList();
                }

                return output;
            }
            catch (Exception ex)
            {
                //_logSystemService.AddExceptionLog(ex);
                return Enumerable.Empty<CountryInfoDTO>().ToList();
            }
        }
        public async Task<AppResponse<EAppResponse, CountryDTO>> CountryInformation(string countryCode)
        {
            var output = default(CountryDTO);

            if (string.IsNullOrWhiteSpace(countryCode))
                return AppResponse.Create(EAppResponse.InvalidInput, output);

            try
            {
                var country = await _countryRepository.GetCountryDetails(countryCode);
                if(country !=null)
                    output = (await MapCountriesContent(new List<Country> { country })).FirstOrDefault();

                return AppResponse.Create(EAppResponse.Success, output);
            }
            catch (Exception ex)
            {
                //_logSystemService.AddExceptionLog(ex);
                return AppResponse.Create(EAppResponse.UnhandledError, output);
            }
        }
        public async Task<AppResponse<EAppResponse, RegionDTO>> RegionInformation(string regionName)
        {
            var output = default(RegionDTO);

            if (string.IsNullOrWhiteSpace(regionName))
                return AppResponse.Create(EAppResponse.InvalidInput, output);

            try
            {
                var regionDetails = await _countryRepository.GetRegionDetails(regionName);
                if (regionDetails != null && regionDetails.Any())
                {
                    output = new RegionDTO()
                    {
                        Name = regionName,
                        SubRegions = await MapSubRegionsContent(regionDetails),
                    };
                }

                return AppResponse.Create(EAppResponse.Success, output);
            }
            catch (Exception ex)
            {
                //_logSystemService.AddExceptionLog(ex);
                return AppResponse.Create(EAppResponse.UnhandledError, output);
            }
        }
        public async Task<AppResponse<EAppResponse, SubRegionDTO>> SubRegionInformation(string regionName)
        {
            var output = default(SubRegionDTO);

            if (string.IsNullOrWhiteSpace(regionName))
                return AppResponse.Create(EAppResponse.InvalidInput, output);

            try
            {
                var subRegionDetails = await _countryRepository.GetRegionDetails(regionName);
                if (subRegionDetails != null && subRegionDetails.Any())
                {
                    output = new SubRegionDTO()
                    {
                        Name = regionName,
                        Countries = await MapCountriesContent(subRegionDetails),
                    };
                }

                return AppResponse.Create(EAppResponse.Success, output);
            }
            catch (Exception ex)
            {
                //_logSystemService.AddExceptionLog(ex);
                return AppResponse.Create(EAppResponse.UnhandledError, output);
            }
        }

        #region Helper
        private async Task<List<CountryDTO>> MapCountriesContent(List<Country> countries)
        {
            var output = Enumerable.Empty<CountryDTO>().ToList();
            var countryList = await _countryRepository.GetCountries();

            foreach(var country in countries)
            {
                var borders = (countryList.Where(row => country.borders.Contains(row.cca3))
                    .Select(row => row.name.common)
                    .ToList()) ?? Enumerable.Empty<string>().ToList();

                output.Add(new CountryDTO()
                {
                    Code = country.cca2.CleanSpace(),
                    Name = country.name.common,
                    CapitalCities = country.capital,
                    Population = country.population,
                    BorderCountries = borders,
                    Flag = country.flags.png,
                    Region = country.region,
                    SubRegion = country.subregion,
                    Currencies = Enumerable.Empty<CurrencyDTO>().ToList(),
                    Languages = Enumerable.Empty<LanguageDTO>().ToList()
                });
            }
           
            return output;
        }
        private async Task<List<SubRegionDTO>> MapSubRegionsContent(List<Country> countries)
        {
            var output = Enumerable.Empty<SubRegionDTO>().ToList();
            var subRegions = countries.GroupBy(row => row.subregion).ToList();
            foreach(var item in subRegions)
            {
                output.Add(new SubRegionDTO()
                {
                    Name = item.Key,
                    RegionName = item.FirstOrDefault()?.region ?? string.Empty,
                    Countries = await MapCountriesContent(item.ToList())
                });
            }

            return output;
        }
        #endregion
    }
}