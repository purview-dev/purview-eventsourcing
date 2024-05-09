﻿using Purview.EventSourcing.MongoDb.Entities;

namespace Purview.EventSourcing.MongoDb.StorageClients;

sealed class BatchOperation
{
	readonly List<TableTransactionAction> _operations = [];

	public bool Any() => _operations.Count > 0;

	public void Delete(IEntity document)
		=> AddAction(TransactionActionType.Delete, document);

	public void Add(IEntity document)
		=> AddAction(TransactionActionType.Add, document);

	public void Update(IEntity document)
		=> AddAction(TransactionActionType.Update, document);

	public void AddAction(TransactionActionType actionType, IEntity document)
	{
		TableTransactionAction tableOperation = new(actionType, document);
		_operations.Add(tableOperation);
	}

	public IEnumerable<TableTransactionAction> GetActions()
		=> _operations;
}

record TableTransactionAction(TransactionActionType ActionType, IEntity Document);
