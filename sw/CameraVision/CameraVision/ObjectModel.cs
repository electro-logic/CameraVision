// Author: Leonardo Tazzini (http://electro-logic.blogspot.it)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

/// <summary>
/// Provides common functionality for ViewModel classes. Allow to easily implement MVVM pattern.
/// </summary>
public abstract class ObjectModel : INotifyPropertyChanged, IDataErrorInfo
{
	public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	protected virtual string OnValidate(string propertyName)
	{
		if (string.IsNullOrEmpty(propertyName))
		{
			throw new ArgumentException("Invalid property name", propertyName);
		}

		string error = string.Empty;

		PropertyInfo propertyInfo = this.GetType().GetProperty(propertyName);
		var value = propertyInfo.GetValue(this, null);
		var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>(1);
		var result = System.ComponentModel.DataAnnotations.Validator.TryValidateProperty(
				value,
				new ValidationContext(this, null, null)
				{
					MemberName = propertyName
				},
				results);

		if (!result)
		{
			var validationResult = results.First();
			error = validationResult.ErrorMessage;
		}

		return error;
	}

	string IDataErrorInfo.Error
	{
		get
		{
			throw new NotSupportedException("IDataErrorInfo.Error is not supported, use IDataErrorInfo.this[propertyName] instead.");
		}
	}

	string IDataErrorInfo.this[string propertyName]
	{
		get
		{
			return OnValidate(propertyName);
		}
	}

}