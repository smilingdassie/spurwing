/*
Author: Roscoe / Andrew
Purpose: Find me the reason why stores are not sending me data.
Modified by: Daniel
Reason: prep for read-only dashboards
Modified Date: 17 Nov 2025 11:11
*/


Use Collect_POS;


declare @TableName varchar(200) = 'Fact.POSInvoice'
declare @IgnoreCollectStatuses table (CollectStatusName varchar(200))
insert into @IgnoreCollectStatuses(CollectStatusName) values('Valid')
insert into @IgnoreCollectStatuses(CollectStatusName) values('Invalid')
insert into @IgnoreCollectStatuses(CollectStatusName) values('Ignored')
insert into @IgnoreCollectStatuses(CollectStatusName) values('Audit')
insert into @IgnoreCollectStatuses(CollectStatusName) values('Delete')

declare @FindCollectStatuses table (CollectStatusName varchar(200))
insert into @FindCollectStatuses(CollectStatusName) values('CollectDisabled')

select 
m.RestaurantKey
,m.RestaurantFullName
,Date = GetDate()
,e.CollectStatus
,EarliestDateKey=MIN (e.DateKey) 
,LatestDateKey=MAX (e.DateKey) 
from Fact.ETL e with (FORCESEEK,NOLOCK)
left join Fact.POSInvoice f with (FORCESEEK,NOLOCK) 
	on f.RAWID = e.RAWID and f.RAWIDLine = e.RAWIDLine
left join RawData.Pilot_SALESMASTER r with (FORCESEEK,NOLOCK) 
	on r.RAWID = e.RAWID and r.RAWIDLine = e.RAWIDLine
left join MasterView.Restaurant m 
	on m.RestaurantKey = e.RestaurantKey
where e.TableName = @TableName
and e.CollectStatus NOT in  (select CollectStatusName from @IgnoreCollectStatuses)
--and e.CollectStatus in (select CollectStatusName from @FindCollectStatuses)
group by 
	m.RestaurantKey,
	m.RestaurantFullName, 
	e.CollectStatus
order by 
	EarliestDateKey DESC, 
	m.RestaurantKey, 
	m.RestaurantFullName, 
	e.CollectStatus
