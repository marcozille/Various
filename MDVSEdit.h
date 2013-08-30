#pragma once


// CMDVSEdit

class CMDVSEdit : public CEdit
{
	DECLARE_DYNAMIC(CMDVSEdit)

public:
	CMDVSEdit();
	virtual ~CMDVSEdit();

protected:
	DECLARE_MESSAGE_MAP()

	bool m_bHover;

public:
	afx_msg void OnNcPaint();
	afx_msg void OnMouseHover(UINT nFlags, CPoint point);
	afx_msg void OnMouseLeave();
	afx_msg void OnMouseMove(UINT nFlags, CPoint point);
};


