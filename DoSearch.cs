public SearchEnvelope DoSearch(Search search, int? gridPage)
{
	SearchEnvelope searchEnvelope = new SearchEnvelope();
	searchEnvelope.Products = new List<Product>();

	using (var db = GetDBContext())
	{
		IQueryable<Entities.Product> query = db.Products.Include(b => b.Division).Include(c => c.ContractingOtherParties).AsNoTracking();

		if (search.ContractingParty != null)
		{
			query = query.Where(GlobalSearchByContractingPartyNameBaseQuery(search.ContractingParty));
		}

		if (search.OtherPartyName != null)
		{
			query = query.Where(GlobalSearchByOtherPartyNameBaseQuery(search.OtherPartyName));
		}

		if (search.SubjectMatter != null)
		{
			query = query.Where(GlobalSearchByProductSubjectMatterBaseQuery(search.SubjectMatter));
		}

		foreach (SearchFilter searchFilter in search.SearchFilters)
		{
			switch (searchFilter.Criterion)
			{
				case FilterCriterion.ProductClassification:
					query = query.Where(GlobalSearchByProductClassificationBaseQuery(searchFilter.SelectedClassificationId, searchFilter.Operator));
					break;

				case FilterCriterion.Status:
					query = query.Where(GlobalSearchByStatusBaseQuery(searchFilter.SelectedStatus, searchFilter.Operator));
					break;

				case FilterCriterion.System:
					query = query.Where(GlobalSearchBySystemBaseQuery(searchFilter.SelectedSystem, searchFilter.Operator));
					break;

				case FilterCriterion.ExpirationDate:
					query = query.Where(GlobalSearchByExpirationDateBaseQuery(searchFilter.ExpirationDate, searchFilter.ExpiryDateOptions));
					break;
			}
		}

		var args = new ListRequestArguments()
		{
			SortBy = "Id:DESC",
			Offset = gridPage == null ? 0 : ((gridPage - 1) * AjaxGridPager.DefaultPageSize),
			Limit = AjaxGridPager.DefaultPageSize
		};

		query = query.ApplySorting(args, e => e.Id);

		searchEnvelope.Count = query.Count();

		query = query.Skip((int)args.Offset).Take((int)args.Limit);
	  
		searchEnvelope.Products = query.Select(a => new Product
		{
			Id = a.Id,
			ContractingOtherParties = a.ContractingOtherParties.Select(op => new ContractingOtherParty
			{
				Id = op.Id,
				FullName = op.FullName
			}),                   
			FullContactName = a.FullContactName,
			ContractingParty = a.ContractingParty,
			Status = a.Status,
			System = a.System,
			EffectiveDate = a.EffectiveDate,
			ExpirationDate = a.ExpirationDate,                    
			DocumentCount = a.Documents.Count,
			SubjectMatter = a.SubjectMatter,
			
		}).ToList();

		return searchEnvelope;
	}
}