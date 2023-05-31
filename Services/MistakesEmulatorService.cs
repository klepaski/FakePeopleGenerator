using Bogus;
using task5.Models;
using CsvHelper;
using System.Globalization;
using System.Runtime.Serialization;

namespace task5.Services
{
    public interface IMistakesEmulatorService
    {
        public List<PersonModel> CorruptData(List<PersonModel> persons, int amountOfMistakes, Region region);
    }

    public class MistakesEmulatorService : IMistakesEmulatorService
    {
        private int _currentAmountOfMistakes { get; set; } = 0;
        private Region _currentRegion { get; set; }
        private string _currentFieldToModify = "";
        private Faker _faker = new();

        private const int MAX_SWITCH_PROBABILITY = 800;
        private const int MAX_DELETE_PROBABILITY = 850;
        private const int MAX_ADD_PROBABILITY = 850;
        private int _currentSwitchProbability = MAX_SWITCH_PROBABILITY;
        private int _currentDeleteProbability = MAX_SWITCH_PROBABILITY;
        private int _currentAddProbability = MAX_SWITCH_PROBABILITY;

        public List<PersonModel> CorruptData(List<PersonModel> persons, int amountOfMistakes, Region region)
        {
            _currentAmountOfMistakes = amountOfMistakes;
            _currentRegion = region;
            List<PersonModel> resultPersons = new();
            foreach (PersonModel person in persons)
            {
                for (int i = 0; i < _currentAmountOfMistakes; i++)
                {
                    switch (_faker.Random.Int(1, 3))
                    {
                        case 1: person.Name = AddMistakeToPersonData(person.Name); _currentFieldToModify = "Name";  break;
                        case 2: person.Address = AddMistakeToPersonData(person.Address); _currentFieldToModify = "Address"; break;
                        case 3: person.Phone = AddMistakeToPersonData(person.Phone); _currentFieldToModify = "Phone"; break;
                    }
                }
                resultPersons.Add(person);
            }
            _currentSwitchProbability = MAX_SWITCH_PROBABILITY;
            _currentDeleteProbability = MAX_DELETE_PROBABILITY;
            _currentAddProbability = MAX_ADD_PROBABILITY;
            return resultPersons;
        }

        private string AddMistakeToPersonData(string dataToModify)
        {
            int rolledNum = _faker.Random.Int(1, MAX_ADD_PROBABILITY);
            int mistakeNumber = (rolledNum <= _currentSwitchProbability) ? 0 : (rolledNum <= _currentDeleteProbability) ? 1 : 2;
            return (mistakeNumber switch
            {
                0 => MakeSwitchedCharsMistake(dataToModify),
                1 => MakeDeletedCharMistake(dataToModify),
                2 => MakeAdditionalCharMistake(dataToModify),
                _ => "",
            });
        }

        private string MakeDeletedCharMistake(string data)
        {
            if (_currentDeleteProbability > _currentSwitchProbability + 1) _currentDeleteProbability--;
            int indexToDelete = _faker.Random.Int(0, data.Length - 1);
            data = data.Remove(indexToDelete, 1);
            return data;
        }

        private string MakeAdditionalCharMistake(string data)
        {
            if (_currentDeleteProbability < _currentAddProbability - 1) _currentDeleteProbability++;
            int indexToAdd = _faker.Random.Int(0, data.Length - 1);
            string characterToAdd = GenerateAlphaNumeric();
            data = data.Insert(indexToAdd, characterToAdd);
            return data;
        }

        private string MakeSwitchedCharsMistake(string data)
        {
            if (data.Length < 2) return data;
            int firstIndex = _faker.Random.Int(0, data.Length - 2);
            int secondIndex = firstIndex + 1;
            string switchedPart = String.Join("", data[secondIndex], data[firstIndex]);
            data = data.Remove(firstIndex, 2).Insert(firstIndex, switchedPart);
            return data;
        }

        private string GenerateAlphaNumeric()
        {
            string chars = _currentRegion switch
            {
                Region.en => "abcdefghijklmnopqrstuvwxyz",
                Region.ru => "абвгдежзийклмнопрстуфхцчшщъыьэюя",
                Region.ko => "ㄱㄴㄷㄹㅁㅂㅅㅇㅈㅊㅋㅌㅍㅎㅏㅑㅓㅕㅗㅛㅜㅠㅡㅣㄲㄸㅃㅉㅆㅢㅚㅐㅟㅔㅒㅖㅘㅝㅙㅞ",
            };
            if (_currentFieldToModify == "Phone") chars = "0123456789";
            int charIndex = _faker.Random.Int(0, chars.Length - 1);
            return chars[charIndex].ToString();
        }
    }
}
