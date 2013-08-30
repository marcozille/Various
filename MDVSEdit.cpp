// MDVSEdit.cpp : file di implementazione
//

#include "stdafx.h"
#include "MFCApplication1.h"
#include "MDVSEdit.h"


// CMDVSEdit

IMPLEMENT_DYNAMIC(CMDVSEdit, CEdit)

CMDVSEdit::CMDVSEdit()
{
	m_bHover = false;
}

CMDVSEdit::~CMDVSEdit()
{
}


BEGIN_MESSAGE_MAP(CMDVSEdit, CEdit)
	ON_WM_NCPAINT()
	ON_WM_MOUSEHOVER()
	ON_WM_MOUSELEAVE()
	ON_WM_MOUSEMOVE()
END_MESSAGE_MAP()


void CMDVSEdit::OnNcPaint()
{
	CDC* pDC = GetWindowDC( );
		
	//work out the coordinates of the window rectangle,
	CRect rect;
	GetWindowRect( &rect);
	ScreenToClient(rect);
	rect.OffsetRect(-rect.left, -rect.top);
	rect.left++;
	rect.top++;

	HWND focusedHwnd = ::GetFocus();
	HBRUSH retVal;
	CPen pen;
	CPen* pOldPen;

	if(m_hWnd == focusedHwnd)
		pen.CreatePen(PS_SOLID, 2, RGB(110,50,157));
	else if(m_bHover)
		pen.CreatePen(PS_SOLID, 2, RGB(220,220,220));
	else
		pen.CreatePen(PS_SOLID, 2, RGB(180,180,180));

	pOldPen = pDC->SelectObject(&pen);

	pDC->Rectangle(rect);

	pDC->SelectObject(pOldPen);

	ReleaseDC( pDC);
}

void CMDVSEdit::OnMouseHover(UINT nFlags, CPoint point)
{
	// TODO: aggiungere qui il codice per la gestione dei messaggi e/o chiamare il codice predefinito.
	//m_bHover = true;
	m_bHover = true;
	CEdit::OnMouseHover(nFlags, point);
	Invalidate();
}


void CMDVSEdit::OnMouseLeave()
{
	// TODO: aggiungere qui il codice per la gestione dei messaggi e/o chiamare il codice predefinito.
	//m_bHover = false;
	m_bHover = false;
	CEdit::OnMouseLeave();
}


void CMDVSEdit::OnMouseMove(UINT nFlags, CPoint point)
{
	TRACKMOUSEEVENT tme;
    tme.cbSize = sizeof(tme);
    tme.hwndTrack = m_hWnd;
    tme.dwFlags = TME_LEAVE|TME_HOVER;
    tme.dwHoverTime = 1;
    TrackMouseEvent(&tme);

	CEdit::OnMouseMove(nFlags, point);
}
